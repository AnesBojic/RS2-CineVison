import '../core/utils/utc_datetime.dart';

class DashboardStats {

  final num totalRevenue;

  final int totalTicketsSold;

  final int totalReservations;

  final int totalCustomers;

  final int totalMovies;

  final int activeMovies;

  final int totalScreenings;

  final int upcomingScreenings;

  final double averageOccupancyPercent;

  final List<MoviePerformance> topMovies;



  DashboardStats({

    required this.totalRevenue,

    required this.totalTicketsSold,

    required this.totalReservations,

    required this.totalCustomers,

    required this.totalMovies,

    required this.activeMovies,

    required this.totalScreenings,

    required this.upcomingScreenings,

    required this.averageOccupancyPercent,

    required this.topMovies,

  });



  factory DashboardStats.fromJson(Map<String, dynamic> json) {

    return DashboardStats(

      totalRevenue: json['totalRevenue'] as num? ?? 0,

      totalTicketsSold: json['totalTicketsSold'] as int? ?? 0,

      totalReservations: json['totalReservations'] as int? ?? 0,

      totalCustomers: json['totalCustomers'] as int? ?? 0,

      totalMovies: json['totalMovies'] as int? ?? 0,

      activeMovies: json['activeMovies'] as int? ?? 0,

      totalScreenings: json['totalScreenings'] as int? ?? 0,

      upcomingScreenings: json['upcomingScreenings'] as int? ?? 0,

      averageOccupancyPercent:

          (json['averageOccupancyPercent'] as num?)?.toDouble() ?? 0,

      topMovies: (json['topMovies'] as List<dynamic>? ?? [])

          .map((e) => MoviePerformance.fromJson(e as Map<String, dynamic>))

          .toList(),

    );

  }

}



class MoviePerformance {
  final int movieId;
  final String title;
  final int screeningsCount;
  final int reservationsCount;
  final int ticketsSold;
  final num revenue;
  final double occupancyPercent;
  final double? avgRating;
  final String? posterImageBase64;

  MoviePerformance({
    required this.movieId,
    required this.title,
    required this.screeningsCount,
    required this.reservationsCount,
    required this.ticketsSold,
    required this.revenue,
    required this.occupancyPercent,
    this.avgRating,
    this.posterImageBase64,
  });

  factory MoviePerformance.fromJson(Map<String, dynamic> json) {
    return MoviePerformance(
      movieId: json['movieId'] as int? ?? 0,
      title: json['title'] as String? ?? '',
      screeningsCount: json['screeningsCount'] as int? ?? 0,
      reservationsCount: json['reservationsCount'] as int? ?? 0,
      ticketsSold: json['ticketsSold'] as int? ?? 0,
      revenue: json['revenue'] as num? ?? 0,
      occupancyPercent: (json['occupancyPercent'] as num?)?.toDouble() ?? 0,
      avgRating: (json['avgRating'] as num?)?.toDouble(),
      posterImageBase64: json['posterImageBase64'] as String?,
    );
  }
}



class TimeSlotPerformance {

  final String timeSlot;

  final int ticketsSold;

  final double occupancyPercent;

  final num revenue;



  TimeSlotPerformance({

    required this.timeSlot,

    required this.ticketsSold,

    required this.occupancyPercent,

    required this.revenue,

  });



  factory TimeSlotPerformance.fromJson(Map<String, dynamic> json) {

    return TimeSlotPerformance(

      timeSlot: json['timeSlot'] as String? ?? '',

      ticketsSold: json['ticketsSold'] as int? ?? 0,

      occupancyPercent: (json['occupancyPercent'] as num?)?.toDouble() ?? 0,

      revenue: json['revenue'] as num? ?? 0,

    );

  }

}



class HallUtilization {

  final int hallId;

  final String hallName;

  final int capacity;

  final int screeningsCount;

  final int showCount;

  final double sharePercent;

  final int seatsOffered;

  final int seatsSold;

  final double utilizationPercent;



  HallUtilization({

    required this.hallId,

    required this.hallName,

    required this.capacity,

    required this.screeningsCount,

    required this.showCount,

    required this.sharePercent,

    required this.seatsOffered,

    required this.seatsSold,

    required this.utilizationPercent,

  });



  factory HallUtilization.fromJson(Map<String, dynamic> json) {

    return HallUtilization(

      hallId: json['hallId'] as int? ?? 0,

      hallName: json['hallName'] as String? ?? '',

      capacity: json['capacity'] as int? ?? 0,

      screeningsCount: json['screeningsCount'] as int? ?? 0,

      showCount: json['showCount'] as int? ?? 0,

      sharePercent: (json['sharePercent'] as num?)?.toDouble() ?? 0,

      seatsOffered: json['seatsOffered'] as int? ?? 0,

      seatsSold: json['seatsSold'] as int? ?? 0,

      utilizationPercent:

          (json['utilizationPercent'] as num?)?.toDouble() ?? 0,

    );

  }

}



class AnalyticsLiveSnapshot {

  final DashboardStats dashboard;

  final List<MoviePerformance> moviePerformance;

  final List<TimeSlotPerformance> timeSlotPerformance;

  final List<HallUtilization> hallUtilization;

  final DateTime? updatedAt;



  AnalyticsLiveSnapshot({

    required this.dashboard,

    required this.moviePerformance,

    required this.timeSlotPerformance,

    required this.hallUtilization,

    this.updatedAt,

  });



  factory AnalyticsLiveSnapshot.fromJson(Map<String, dynamic> json) {

    return AnalyticsLiveSnapshot(

      dashboard: DashboardStats.fromJson(

        Map<String, dynamic>.from(json['dashboard'] as Map? ?? {}),

      ),

      moviePerformance: (json['moviePerformance'] as List<dynamic>? ?? [])

          .map((e) => MoviePerformance.fromJson(e as Map<String, dynamic>))

          .toList(),

      timeSlotPerformance: (json['timeSlotPerformance'] as List<dynamic>? ?? [])

          .map((e) => TimeSlotPerformance.fromJson(e as Map<String, dynamic>))

          .toList(),

      hallUtilization: (json['hallUtilization'] as List<dynamic>? ?? [])

          .map((e) => HallUtilization.fromJson(e as Map<String, dynamic>))

          .toList(),

      updatedAt: UtcDateTime.tryParse(json['updatedAt']),

    );

  }

}


