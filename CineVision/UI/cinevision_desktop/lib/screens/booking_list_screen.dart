import 'package:cinevision_desktop/core/enums/api_enums.dart';
import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/reservation.dart';
import 'package:cinevision_desktop/providers/reservation_provider.dart';
import 'package:cinevision_desktop/utils/api_client_exception.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

/// Staff view of bookings so cancellation / refund reasons can be audited.
class BookingListScreen extends StatefulWidget {
  const BookingListScreen({super.key});

  @override
  State<BookingListScreen> createState() => _BookingListScreenState();
}

class _BookingListScreenState extends State<BookingListScreen> {
  late ReservationProvider _provider;
  List<Reservation> _items = [];
  bool _loading = true;
  static const int _pageSize = 10;
  int _page = 1;
  int _totalCount = 0;
  String? _statusFilter;
  String? _refundFilter;
  int? _projectionId;
  int? _retryingId;
  final _searchController = TextEditingController();
  int _seenLiveTick = 0;

  int get _totalPages =>
      _totalCount == 0 ? 1 : (_totalCount / _pageSize).ceil();

  @override
  void initState() {
    super.initState();
    _provider = context.read<ReservationProvider>();
    _seenLiveTick = _provider.liveTick;
    _projectionId = _provider.focusProjectionId;
    _provider.addListener(_onProviderChanged);
    _load();
  }

  @override
  void dispose() {
    _provider.removeListener(_onProviderChanged);
    _searchController.dispose();
    super.dispose();
  }

  void _onProviderChanged() {
    if (!mounted) return;
    final focused = _provider.focusProjectionId;
    if (focused != null && focused != _projectionId) {
      setState(() {
        _projectionId = focused;
        _statusFilter = null;
        _page = 1;
      });
      _load();
      return;
    }
    if (_provider.liveTick == _seenLiveTick) return;
    _seenLiveTick = _provider.liveTick;
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final filter = <String, dynamic>{
        'page': _page,
        'pageSize': _pageSize,
        'includeTotalCount': true,
      };
      if (_statusFilter != null) {
        filter['status'] = int.parse(_statusFilter!);
      }
      if (_refundFilter != null) {
        filter['refundStatus'] = int.parse(_refundFilter!);
      }
      if (_projectionId != null) {
        filter['projectionId'] = _projectionId;
      }
      final query = _searchController.text.trim();
      if (query.isNotEmpty) {
        filter['query'] = query;
      }
      final data = await _provider.get(filter: filter);
      if (!mounted) return;
      setState(() {
        _items = data.items ?? [];
        _totalCount = data.totalCount ?? _items.length;
        _loading = false;
      });
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Bookings',
      isLoading: _loading,
      toolbar: Row(
        children: [
          FilterDropdown(
            hint: 'All statuses',
            value: _statusFilter,
            items: const [
              DropdownMenuItem(value: null, child: Text('All statuses')),
              DropdownMenuItem(
                value: '${ReservationStatus.cancelled}',
                child: Text('Cancelled'),
              ),
              DropdownMenuItem(
                value: '${ReservationStatus.paid}',
                child: Text('Paid'),
              ),
              DropdownMenuItem(
                value: '${ReservationStatus.completed}',
                child: Text('Completed'),
              ),
              DropdownMenuItem(
                value: '${ReservationStatus.pending}',
                child: Text('Pending'),
              ),
              DropdownMenuItem(
                value: '${ReservationStatus.confirmed}',
                child: Text('Confirmed'),
              ),
            ],
            onChanged: (v) {
              setState(() {
                _statusFilter = v;
                _page = 1;
              });
              _load();
            },
          ),
          const SizedBox(width: 10),
          FilterDropdown(
            hint: 'All refunds',
            value: _refundFilter,
            items: const [
              DropdownMenuItem(value: null, child: Text('All refunds')),
              DropdownMenuItem(
                value: '${RefundStatus.failed}',
                child: Text('Refund failed'),
              ),
              DropdownMenuItem(
                value: '${RefundStatus.pending}',
                child: Text('Refund pending'),
              ),
              DropdownMenuItem(
                value: '${RefundStatus.refunded}',
                child: Text('Refunded'),
              ),
            ],
            onChanged: (v) {
              setState(() {
                _refundFilter = v;
                _page = 1;
              });
              _load();
            },
          ),
          const SizedBox(width: 10),
          SearchField(
            controller: _searchController,
            hint: 'Search number, movie, customer...',
            width: 320,
            onSubmitted: (_) {
              setState(() => _page = 1);
              _load();
            },
          ),
          if (_projectionId != null) ...[
            const SizedBox(width: 10),
            InputChip(
              label: Text('Projection #$_projectionId'),
              onDeleted: () {
                _provider.clearProjectionFocus();
                setState(() {
                  _projectionId = null;
                  _page = 1;
                });
                _load();
              },
            ),
          ],
        ],
      ),
      child: Column(
        children: [
          _BookingStatusInfoBox(),
          const SizedBox(height: 12),
          Expanded(
            child: DataCard(
              emptyMessage: _items.isEmpty ? 'No bookings found' : null,
              child: StyledDataTable(
                columns: const [
                  DataColumn(label: Text('Ticket')),
                  DataColumn(label: Text('Customer')),
                  DataColumn(label: Text('Movie')),
                  DataColumn(label: Text('Show')),
                  DataColumn(label: Text('Status')),
                  DataColumn(label: Text('Refund')),
                  DataColumn(label: Text('Cancellation reason')),
                  actionsDataColumn,
                ],
                rows: _items.map(_buildRow).toList(),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'Page $_page of $_totalPages · $_totalCount bookings',
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(width: 12),
              IconButton(
                tooltip: 'Previous page',
                onPressed: _page > 1 && !_loading
                    ? () {
                        setState(() => _page--);
                        _load();
                      }
                    : null,
                icon: const Icon(Icons.chevron_left),
              ),
              IconButton(
                tooltip: 'Next page',
                onPressed: _page < _totalPages && !_loading
                    ? () {
                        setState(() => _page++);
                        _load();
                      }
                    : null,
                icon: const Icon(Icons.chevron_right),
              ),
            ],
          ),
        ],
      ),
    );
  }

  DataRow _buildRow(Reservation r) {
    return DataRow(cells: [
      DataCell(Text(
        r.reservationNumber,
        style: const TextStyle(fontWeight: FontWeight.w500),
      )),
      DataCell(Text(
        (r.customerName?.trim().isNotEmpty == true)
            ? r.customerName!
            : (r.customerEmail ?? '—'),
      )),
      DataCell(Text(r.movieTitle.isEmpty ? '—' : r.movieTitle)),
      DataCell(Text(
        '${formatDate(r.projectionStartTime)} ${formatTime(r.projectionStartTime)}',
      )),
      DataCell(_statusBadge(r)),
      DataCell(_refundBadge(r)),
      DataCell(
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 280),
          child: Text(
            r.reasonLabel,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ),
      actionButtonsCell([
        ActionIconButton(
          icon: Icons.info_outline,
          color: AppColors.blue,
          tooltip: 'Details',
          onPressed: () => _showDetails(r),
        ),
        ActionIconButton(
          icon: Icons.currency_exchange,
          color: AppColors.orange,
          tooltip: _refundButtonTooltip(r),
          enabled: r.canRetryRefund && _retryingId != r.id,
          onPressed: () => _retryRefund(r),
        ),
      ]),
    ]);
  }

  String _refundButtonTooltip(Reservation r) {
    if (_retryingId == r.id) return 'Retrying refund…';
    return switch (r.refundStatus) {
      RefundStatus.failed => 'Refund',
      RefundStatus.refunded => 'Already refunded',
      RefundStatus.pending => 'Refund is still pending with Stripe',
      _ => 'Refund is available only when Stripe failed',
    };
  }

  Widget _refundBadge(Reservation r) {
    if (r.refundStatus == RefundStatus.none) {
      return const Text('—', style: TextStyle(color: AppColors.textSecondary));
    }
    final color = switch (r.refundStatus) {
      RefundStatus.refunded => AppColors.green,
      RefundStatus.failed => AppColors.primary,
      RefundStatus.pending => AppColors.orange,
      _ => AppColors.textSecondary,
    };
    return StatusBadge(label: r.refundLabel, color: color, filled: true);
  }

  Future<void> _retryRefund(Reservation r) async {
    final ok = await confirmRefund(
      context,
      r.refundStatus == RefundStatus.failed
          ? 'Retry the Stripe refund for ${r.reservationNumber}?'
          : 'Stripe has not confirmed the refund for ${r.reservationNumber}. Try again now?',
    );
    if (ok != true || !mounted) return;

    setState(() => _retryingId = r.id);
    try {
      final updated = await _provider.retryRefund(r.id);
      if (!mounted) return;
      if (updated.refundStatus == RefundStatus.refunded) {
        showAppSnackBar(context, 'Refund completed for ${r.reservationNumber}');
      } else {
        showAppSnackBar(
          context,
          updated.refundError?.trim().isNotEmpty == true
              ? updated.refundError!
              : 'Refund is still ${updated.refundLabel.toLowerCase()}.',
          isError: true,
        );
      }
      await _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Refund retry failed', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    } finally {
      if (mounted) setState(() => _retryingId = null);
    }
  }

  Widget _statusBadge(Reservation r) {
    final color = switch (r.status) {
      ReservationStatus.cancelled => AppColors.orange,
      ReservationStatus.paid => AppColors.green,
      ReservationStatus.completed => AppColors.blue,
      ReservationStatus.pending => AppColors.textSecondary,
      _ => AppColors.purple,
    };
    return StatusBadge(
      label: r.statusName.isEmpty ? 'Unknown' : r.statusName,
      color: color,
      filled: true,
    );
  }

  Future<void> _showDetails(Reservation r) {
    return showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.card,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Text(
          r.reservationNumber,
          style: const TextStyle(color: AppColors.textPrimary),
        ),
        content: SizedBox(
          width: 460,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _detailLine('Customer', r.customerName ?? '—'),
              _detailLine('Email', r.customerEmail ?? '—'),
              _detailLine('Movie', r.movieTitle),
              _detailLine('Hall', r.hallName),
              _detailLine(
                'Show',
                '${formatDate(r.projectionStartTime)} ${formatTime(r.projectionStartTime)}',
              ),
              _detailLine('Amount', formatCurrency(r.totalAmount)),
              _detailLine('Status', r.statusName),
              _detailLine('Refund', r.refundLabel),
              if (r.refundError != null && r.refundError!.trim().isNotEmpty)
                _detailLine('Refund error', r.refundError!),
              if (r.status == ReservationStatus.pending && r.holdExpiresAt != null)
                _detailLine(
                  'Hold until',
                  '${formatDate(r.holdExpiresAt)} ${formatTime(r.holdExpiresAt)}',
                ),
              if (r.cancelledAt != null)
                _detailLine(
                  'Cancelled',
                  '${formatDate(r.cancelledAt)} ${formatTime(r.cancelledAt)}',
                ),
              const SizedBox(height: 12),
              const Text(
                'Cancellation reason',
                style: TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                r.reasonLabel,
                style: const TextStyle(color: AppColors.textPrimary),
              ),
            ],
          ),
        ),
        actions: [
          if (r.canRetryRefund)
            TextButton(
              onPressed: () {
                Navigator.pop(context);
                _retryRefund(r);
              },
              child: const Text('Retry refund'),
            ),
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  Widget _detailLine(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(
              label,
              style: const TextStyle(color: AppColors.textSecondary, fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(color: AppColors.textPrimary, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }
}

class _BookingStatusInfoBox extends StatelessWidget {
  const _BookingStatusInfoBox();

  static const _items = [
    (label: 'Pending', detail: 'Checkout hold — seats free if payment is cancelled'),
    (label: 'Confirmed', detail: 'Sold at the counter'),
    (label: 'Paid', detail: 'Paid online, ticket still valid'),
    (label: 'Completed', detail: 'Show already happened'),
    (label: 'Cancelled', detail: 'Voided — check Refund column; retry if Stripe failed'),
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(14, 10, 14, 10),
      decoration: AppDecorations.subtleBorder(radius: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.only(top: 1),
            child: Icon(Icons.info_outline, size: 16, color: AppColors.blue),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Wrap(
              spacing: 18,
              runSpacing: 6,
              children: [
                for (final item in _items)
                  Text.rich(
                    TextSpan(
                      children: [
                        TextSpan(
                          text: '${item.label}: ',
                          style: const TextStyle(
                            color: AppColors.textPrimary,
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        TextSpan(
                          text: item.detail,
                          style: const TextStyle(
                            color: AppColors.textSecondary,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          if (context.watch<ReservationProvider>().isLiveConnected)
            const Padding(
              padding: EdgeInsets.only(left: 8, top: 1),
              child: StatusBadge(label: 'Live', color: AppColors.green, filled: true),
            ),
        ],
      ),
    );
  }
}
