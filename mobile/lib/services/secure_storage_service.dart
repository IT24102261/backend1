import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:fixflow_mobile/models/models.dart';

class SecureStorageService {
  SecureStorageService({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;
  static const _access = 'fixflow.accessToken';
  static const _refresh = 'fixflow.refreshToken';
  static const _user = 'fixflow.user';

  Future<void> saveSession(AuthSession session, [UserProfile? user]) async {
    await _storage.write(key: _access, value: session.accessToken);
    await _storage.write(key: _refresh, value: session.refreshToken);
    if (user != null) {
      await _storage.write(key: _user, value: jsonEncode(user.toJson()));
    }
  }

  Future<void> saveUser(UserProfile user) =>
      _storage.write(key: _user, value: jsonEncode(user.toJson()));

  Future<String?> readToken() => _storage.read(key: _access);

  Future<String?> readRefreshToken() => _storage.read(key: _refresh);

  Future<UserProfile?> readUser() async {
    final raw = await _storage.read(key: _user);
    if (raw == null) return null;
    try {
      return UserProfile.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return null;
    }
  }

  Future<void> clear() async {
    await _storage.delete(key: _access);
    await _storage.delete(key: _refresh);
    await _storage.delete(key: _user);
  }
}
