import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/services/api_client.dart';
import 'package:fixflow_mobile/services/fixflow_api.dart';
import 'package:fixflow_mobile/services/secure_storage_service.dart';

final secureStorageProvider = Provider<SecureStorageService>((ref) => SecureStorageService());

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(ref.watch(secureStorageProvider)));

final apiProvider = Provider<FixFlowApi>((ref) => FixFlowApi(ref.watch(apiClientProvider)));

class AuthNotifier extends AsyncNotifier<UserProfile?> {
  @override
  Future<UserProfile?> build() => _hydrate();

  Future<UserProfile?> _hydrate() async {
    final storage = ref.read(secureStorageProvider);
    final token = await storage.readToken();
    if (token == null) return null;
    try {
      final user = await ref.read(apiProvider).me();
      await storage.saveUser(user);
      return user;
    } catch (_) {
      await storage.clear();
      return null;
    }
  }

  Future<UserProfile> login(String email, String password) async {
    final api = ref.read(apiProvider);
    final storage = ref.read(secureStorageProvider);
    final session = await api.login(email, password);
    await storage.saveSession(session);
    final user = await api.me();
    await storage.saveUser(user);
    state = AsyncData(user);
    return user;
  }

  Future<UserProfile?> register({
    required String email,
    required String password,
    required String displayName,
    required String role,
    String? phone,
    String? address,
    String? categoryId,
    String? nicPhotoPath,
    String? nicPhotoName,
    String? certificatePath,
    String? certificateName,
    String? profilePhotoPath,
    String? profilePhotoName,
  }) async {
    final api = ref.read(apiProvider);
    final storage = ref.read(secureStorageProvider);
    final session = await api.register(
      email: email,
      password: password,
      displayName: displayName,
      role: role,
      phone: phone,
      address: address,
      categoryId: categoryId,
      nicPhotoPath: nicPhotoPath,
      nicPhotoName: nicPhotoName,
      certificatePath: certificatePath,
      certificateName: certificateName,
      profilePhotoPath: profilePhotoPath,
      profilePhotoName: profilePhotoName,
    );
    if (session.requiresAdminApproval || session.accessToken.isEmpty) {
      return null;
    }
    await storage.saveSession(session);
    final user = await api.me();
    await storage.saveUser(user);
    state = AsyncData(user);
    return user;
  }

  Future<void> logout() async {
    final storage = ref.read(secureStorageProvider);
    final refresh = await storage.readRefreshToken();
    if (refresh != null) {
      try {
        await ref.read(apiProvider).logout(refresh);
      } catch (_) {}
    }
    await storage.clear();
    state = const AsyncData(null);
  }
}

final authProvider = AsyncNotifierProvider<AuthNotifier, UserProfile?>(AuthNotifier.new);

final healthStatusProvider = FutureProvider<String>((ref) async {
  try {
    final response = await ref.watch(apiClientProvider).http.get<Map<String, dynamic>>('/health');
    return response.data?['status']?.toString() ?? 'unknown';
  } catch (_) {
    return 'offline';
  }
});
