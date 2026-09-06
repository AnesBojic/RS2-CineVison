/// Client-side mirrors of `CineVision.Model.RolePermissionNames`.
class RolePermissions {
  static const accessDesktop = 'AccessDesktop';
  static const manageUsers = 'ManageUsers';
  static const manageMovies = 'ManageMovies';
  static const manageHalls = 'ManageHalls';
  static const manageProjections = 'ManageProjections';
  static const manageNews = 'ManageNews';
  static const manageReferenceData = 'ManageReferenceData';
  static const manageRoles = 'ManageRoles';
  static const viewAnalytics = 'ViewAnalytics';
  static const useChatBot = 'UseChatBot';

  static const defaultColor = '#64748B';

  static const presets = [
    '#7C3AED',
    '#2563EB',
    '#16A34A',
    '#DC2626',
    '#EA580C',
    '#0D9488',
    '#DB2777',
    '#64748B',
  ];

  static const labels = <String, String>{
    accessDesktop: 'Access desktop app',
    manageUsers: 'Manage users',
    manageMovies: 'Manage movies',
    manageHalls: 'Manage halls',
    manageProjections: 'Manage projections',
    manageNews: 'Manage news',
    manageReferenceData: 'Manage reference data',
    manageRoles: 'Manage roles',
    viewAnalytics: 'View analytics & dashboard',
    useChatBot: 'Use chatbot',
  };

  static const all = [
    accessDesktop,
    manageUsers,
    manageMovies,
    manageHalls,
    manageProjections,
    manageNews,
    manageReferenceData,
    manageRoles,
    viewAnalytics,
    useChatBot,
  ];
}
