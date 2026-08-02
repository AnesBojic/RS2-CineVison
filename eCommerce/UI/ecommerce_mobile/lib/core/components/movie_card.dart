import 'package:ecommerce_mobile/core/components/base64_image.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/providers/movie_provider.dart';
import 'package:ecommerce_mobile/models/movie.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class MovieCard extends StatelessWidget {
  const MovieCard({
    super.key,
    required this.movie,
    this.recommendationReason,
  });

  final Movie movie;
  final String? recommendationReason;

  @override
  Widget build(BuildContext context) {
    final genreName = movie.genre?.name ?? '';
    final duration = movie.durationMinutes ?? 0;

    return Container(
      decoration: BoxDecoration(
        color: AppColors.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: movie.posterImageBase64 != null &&
                    movie.posterImageBase64!.isNotEmpty
                ? Base64ImageWithLoader(
                    movie.posterImageBase64!,
                    radius: 0,
                  )
                : Container(
                    color: AppColors.gray,
                    alignment: Alignment.center,
                    child: const Icon(
                      Icons.movie_outlined,
                      size: 48,
                      color: AppColors.placeholder,
                    ),
                  ),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  movie.title ?? '',
                  style: Theme.of(context).textTheme.titleMedium,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 4),
                Text(
                  [
                    if (genreName.isNotEmpty) genreName,
                    if ((movie.language ?? '').trim().isNotEmpty)
                      movie.language!.trim(),
                    if (duration > 0) '$duration min',
                  ].join(' · '),
                  style: Theme.of(context).textTheme.bodyMedium,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (recommendationReason != null &&
                    recommendationReason!.isNotEmpty) ...[
                  const SizedBox(height: 6),
                  Text(
                    recommendationReason!,
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontSize: 11,
                      height: 1.3,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
                const SizedBox(height: 10),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: () {
                      final id = movie.id;
                      if (id != null) {
                        context.read<MovieProvider>().registerView(id);
                      }
                      Navigator.pushNamed(
                        context,
                        AppRoutes.booking,
                        arguments: movie,
                      );
                    },
                    child: const Text('View Showtimes'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
