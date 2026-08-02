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

  /// Whole number within [min]..[max]. Empty is rejected unless [required] is false.
  static String? integer(
    String? value, {
    String field = 'Field',
    int min = 1,
    int? max,
    bool required = true,
  }) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return required ? '$field is required' : null;
    final parsed = int.tryParse(v);
    if (parsed == null) return '$field must be a whole number';
    if (parsed < min) return '$field must be at least $min';
    if (max != null && parsed > max) return '$field must be $max or less';
    return null;
  }

  /// Positive amount of money. Tolerates a leading currency symbol.
  static String? price(String? value, {String field = 'Price'}) {
    final v = value?.replaceAll('\$', '').trim() ?? '';
    if (v.isEmpty) return '$field is required';
    final parsed = num.tryParse(v);
    if (parsed == null || parsed <= 0) return 'Enter a valid ${field.toLowerCase()}';
    return null;
  }

  /// Exactly [length] digits, e.g. a one-time reset code.
  static String? digitCode(String? value, int length, {String field = 'Code'}) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return '$field is required';
    if (v.length != length || int.tryParse(v) == null) {
      return '$field must be $length digits';
    }
    return null;
  }

  /// Confirmation field that has to repeat [other] exactly.
  static String? match(String? value, String other, {String field = 'Passwords'}) {
    if (value != other) return '$field do not match';
    return null;
  }
}
