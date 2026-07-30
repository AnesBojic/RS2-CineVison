class FieldValidators {
  static final _emailRegex = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  /// Digits with optional leading +, spaces, dashes; 7–15 digits total.
  static final _phoneRegex = RegExp(r'^\+?[0-9][0-9\s\-]{6,18}[0-9]$');

  static String? required(String? value, {String field = 'Field'}) {
    if (value == null || value.trim().isEmpty) return '$field is required';
    return null;
  }

  static String? email(String? value, {bool required = true}) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return required ? 'Email is required' : null;
    if (!_emailRegex.hasMatch(v)) return 'Enter a valid email address';
    return null;
  }

  static String? phone(String? value, {bool required = false}) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return required ? 'Phone number is required' : null;
    final digits = v.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length < 7 || digits.length > 15 || !_phoneRegex.hasMatch(v)) {
      return 'Enter a valid phone number';
    }
    return null;
  }

  static String? minLength(String? value, int min, {String field = 'Field'}) {
    if (value == null || value.length < min) {
      return '$field must be at least $min characters';
    }
    return null;
  }
}
