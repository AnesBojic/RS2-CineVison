import 'package:flutter/material.dart';

class AppColors {
  /// Main content canvas — deep blue-black.
  static const background = Color(0xFF07090F);

  /// Sidebar, tables, stat cards — lifted panel tone.
  static const sidebar = Color(0xFF12141D);
  static const card = Color(0xFF12141D);

  static const cardHover = Color(0xFF181C26);
  static const cardBorder = Color(0xFF1E2430);
  static const divider = Color(0xFF1A1F2A);
  /// Row separators in data tables — ghost line on dark panels.
  static const tableDivider = Color(0x0AFFFFFF);
  static const primary = Color(0xFFE50914);
  static const primaryDark = Color(0xFFB20710);
  static const textPrimary = Color(0xFFF0F2F5);
  static const textSecondary = Color(0xFF8B95A5);
  static const inputFill = Color(0xFF171B24);
  static const green = Color(0xFF22C55E);
  static const blue = Color(0xFF3B82F6);
  static const orange = Color(0xFFF59E0B);
  static const purple = Color(0xFF8B5CF6);
  static const tableHeader = Color(0xFF12141D);
}

class AppDecorations {
  static BoxDecoration card({double radius = 16}) => BoxDecoration(
        color: AppColors.card,
        borderRadius: BorderRadius.circular(radius),
      );

  static BoxDecoration subtleBorder({double radius = 16}) => BoxDecoration(
        color: AppColors.card,
        borderRadius: BorderRadius.circular(radius),
        border: Border.all(color: AppColors.cardBorder.withValues(alpha: 0.3)),
      );
}

class AppTheme {
  static ThemeData get darkTheme {
    return ThemeData(
      brightness: Brightness.dark,
      scaffoldBackgroundColor: AppColors.background,
      fontFamily: 'Segoe UI',
      colorScheme: const ColorScheme.dark(
        primary: AppColors.primary,
        secondary: AppColors.blue,
        surface: AppColors.card,
        error: Color(0xFFEF4444),
        onPrimary: Colors.white,
        onSurface: AppColors.textPrimary,
        outline: AppColors.tableDivider,
        outlineVariant: AppColors.tableDivider,
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.tableDivider,
        thickness: 0.5,
        space: 0,
      ),
      scrollbarTheme: ScrollbarThemeData(
        thumbColor: WidgetStateProperty.all(AppColors.cardBorder.withValues(alpha: 0.55)),
        radius: const Radius.circular(8),
        thickness: WidgetStateProperty.all(5),
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AppColors.card,
        contentTextStyle: const TextStyle(color: AppColors.textPrimary),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        behavior: SnackBarBehavior.floating,
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: AppColors.card,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.inputFill,
        hintStyle: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
        labelStyle: const TextStyle(color: AppColors.textSecondary, fontSize: 13),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: AppColors.cardBorder.withValues(alpha: 0.2)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: AppColors.primary, width: 1.2),
        ),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.textSecondary,
          textStyle: const TextStyle(fontWeight: FontWeight.w500),
        ),
      ),
      dividerColor: AppColors.tableDivider,
      dataTableTheme: DataTableThemeData(
        headingRowColor: WidgetStateProperty.all(AppColors.tableHeader),
        headingTextStyle: const TextStyle(
          color: AppColors.textSecondary,
          fontWeight: FontWeight.w600,
          fontSize: 12,
          letterSpacing: 0.3,
        ),
        dataTextStyle: const TextStyle(color: AppColors.textPrimary, fontSize: 14),
        dataRowMinHeight: 56,
        dataRowMaxHeight: 56,
        columnSpacing: 32,
        horizontalMargin: 24,
        dividerThickness: 0.5,
      ),
    );
  }
}

void showAppSnackBar(BuildContext context, String message, {bool isError = false}) {
  final messenger = ScaffoldMessenger.maybeOf(context);
  if (messenger == null) return;
  messenger.showSnackBar(
    SnackBar(
      content: Row(
        children: [
          Icon(
            isError ? Icons.error_outline : Icons.check_circle_outline,
            color: isError ? AppColors.orange : AppColors.green,
            size: 20,
          ),
          const SizedBox(width: 10),
          Expanded(child: Text(message)),
        ],
      ),
    ),
  );
}
