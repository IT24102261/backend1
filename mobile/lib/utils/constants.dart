import 'dart:io';

class AppConstants {
  static const _envUrl = String.fromEnvironment('API_BASE_URL');

  /// PC Wi-Fi address. A real phone must join this same Wi-Fi (not mobile data).
  static const wifiApiUrl = 'http://192.168.1.26:5080';

  /// PC Ethernet address, USB reverse, and emulator fallbacks.
  static const apiCandidates = [
    wifiApiUrl,
    'http://192.168.0.100:5080',
    'http://127.0.0.1:5080',
    'http://10.0.2.2:5080',
  ];

  static String resolvedUrl = preferredUrl;

  static String get preferredUrl => _envUrl.isNotEmpty
      ? _envUrl
      : Platform.isAndroid
          ? wifiApiUrl
          : 'http://127.0.0.1:5080';

  static String get apiBaseUrl => resolvedUrl;
}
