bool isValidEmail(String? value) {
  final email = value?.trim() ?? '';
  if (email.isEmpty || '@'.allMatches(email).length != 1) return false;
  return RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(email);
}

String phoneDigits(String? value) => (value ?? '').replaceAll(RegExp(r'\D'), '');

bool isValidPhone(String? value) => phoneDigits(value).length == 10;

String? emailValidator(String? value) => isValidEmail(value) ? null : 'Enter a valid email with one @';

String? phoneValidator(String? value, {bool required = false}) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return required ? 'Enter your phone number' : null;
  return isValidPhone(text) ? null : 'Phone number must be 10 digits';
}

bool isFutureDateTime(DateTime value) => value.isAfter(DateTime.now());
