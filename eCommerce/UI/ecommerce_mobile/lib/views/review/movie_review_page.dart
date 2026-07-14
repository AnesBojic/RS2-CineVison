import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/providers/review_provider.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class MovieReviewPage extends StatefulWidget {
  const MovieReviewPage({
    super.key,
    required this.movieId,
    required this.movieTitle,
    this.reviewId,
    this.initialRating,
    this.initialComment,
  });

  final int movieId;
  final String movieTitle;
  final int? reviewId;
  final int? initialRating;
  final String? initialComment;

  @override
  State<MovieReviewPage> createState() => _MovieReviewPageState();
}

class _MovieReviewPageState extends State<MovieReviewPage> {
  final _formKey = GlobalKey<FormState>();
  final _commentController = TextEditingController();
  int _rating = 0;
  bool _saving = false;

  bool get _isEditing => widget.reviewId != null;

  @override
  void initState() {
    super.initState();
    _rating = widget.initialRating ?? 0;
    _commentController.text = widget.initialComment ?? '';
    if (_isEditing && _rating == 0) {
      _loadExisting();
    }
  }

  Future<void> _loadExisting() async {
    try {
      final review =
          await context.read<ReviewProvider>().getReview(widget.reviewId!);
      if (!mounted) return;
      setState(() {
        _rating = review.rating;
        _commentController.text = review.comment ?? '';
      });
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_rating < 1) {
      alertBox(context, 'Rating required', 'Please select a star rating.');
      return;
    }

    setState(() => _saving = true);
    try {
      final provider = context.read<ReviewProvider>();
      if (_isEditing) {
        await provider.updateReview(
          reviewId: widget.reviewId!,
          rating: _rating,
          comment: _commentController.text,
        );
      } else {
        await provider.submitReview(
          movieId: widget.movieId,
          rating: _rating,
          comment: _commentController.text,
        );
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(_isEditing ? 'Review updated' : 'Review submitted'),
        ),
      );
      Navigator.pop(context, true);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: CineAppBar(
        title: _isEditing ? 'Edit Review' : 'Write Review',
        showBack: true,
        showAuthAction: false,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppDefaults.padding),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                widget.movieTitle,
                style: Theme.of(context).textTheme.titleLarge,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              const Text(
                'Share your experience after the screening',
                style: TextStyle(color: AppColors.textSecondary),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 28),
              const Text(
                'Your rating',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(5, (index) {
                  final star = index + 1;
                  return IconButton(
                    onPressed: () => setState(() => _rating = star),
                    icon: Icon(
                      star <= _rating ? Icons.star : Icons.star_border,
                      color: AppColors.primary,
                      size: 36,
                    ),
                  );
                }),
              ),
              const SizedBox(height: 24),
              TextFormField(
                controller: _commentController,
                maxLines: 5,
                maxLength: 1000,
                decoration: const InputDecoration(
                  labelText: 'Comment (optional)',
                  alignLabelWithHint: true,
                ),
              ),
              const SizedBox(height: 28),
              SizedBox(
                height: 48,
                child: ElevatedButton(
                  onPressed: _saving ? null : _submit,
                  child: _saving
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : Text(_isEditing ? 'Save Review' : 'Submit Review'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
