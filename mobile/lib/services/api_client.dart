import 'package:dio/dio.dart';
import 'package:fixflow_mobile/core/api_exception.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/services/secure_storage_service.dart';
import 'package:fixflow_mobile/utils/constants.dart';

class ApiClient {
  ApiClient(this._storage) {
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConstants.apiBaseUrl,
        connectTimeout: const Duration(seconds: 8),
        receiveTimeout: const Duration(seconds: 20),
        headers: {'Content-Type': 'application/json'},
      ),
    );
    _refreshDio = Dio(BaseOptions(baseUrl: AppConstants.apiBaseUrl));
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          try {
            await resolveBaseUrl();
            options.baseUrl = _dio.options.baseUrl;
          } catch (error) {
            handler.reject(
              DioException(requestOptions: options, error: error, type: DioExceptionType.connectionError),
            );
            return;
          }
          final token = await _storage.readToken();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          if (options.data is FormData) {
            options.headers.remove('Content-Type');
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final request = error.requestOptions;
          final alreadyRetried = request.extra['retried'] == true;
          if (error.response?.statusCode != 401 ||
              alreadyRetried ||
              request.path.contains('/api/auth/')) {
            handler.next(error);
            return;
          }
          try {
            final next = await _refresh();
            if (next == null) {
              handler.next(error);
              return;
            }
            request.headers['Authorization'] = 'Bearer $next';
            request.extra['retried'] = true;
            final response = await _dio.fetch(request);
            handler.resolve(response);
          } catch (_) {
            handler.next(error);
          }
        },
      ),
    );
  }

  final SecureStorageService _storage;
  late final Dio _dio;
  late final Dio _refreshDio;

  Dio get http => _dio;
  bool _resolved = false;

  Future<void> resolveBaseUrl() async {
    if (_resolved) return;
    final preferred = AppConstants.preferredUrl;
    final urls = [preferred, ...AppConstants.apiCandidates.where((url) => url != preferred)];
    for (final url in urls) {
      try {
        final probe = Dio(BaseOptions(baseUrl: url, connectTimeout: const Duration(seconds: 3), receiveTimeout: const Duration(seconds: 3)));
        final response = await probe.get<Map<String, dynamic>>('/api/health');
        if (response.statusCode == 200) {
          AppConstants.resolvedUrl = url;
          _dio.options.baseUrl = url;
          _refreshDio.options.baseUrl = url;
          _resolved = true;
          return;
        }
      } catch (_) {}
    }
    throw const ApiException(
      'Cannot reach the FixFlow API. Connect this phone to the same Wi-Fi as the PC and turn off mobile data.',
    );
  }

  Future<String?> _refresh() async {
    final refresh = await _storage.readRefreshToken();
    if (refresh == null) return null;
    final response = await _refreshDio.post<Map<String, dynamic>>(
      '/api/auth/refresh',
      data: {'refreshToken': refresh},
    );
    final session = AuthSession.fromJson(response.data ?? {});
    final user = await _storage.readUser();
    await _storage.saveSession(session, user);
    return session.accessToken;
  }

  Never throwFrom(DioException error) {
    final data = error.response?.data;
    if (data is Map && data['error'] != null) {
      throw ApiException(
        data['error'].toString(),
        code: data['code']?.toString(),
        statusCode: error.response?.statusCode,
      );
    }
    if (error.response?.statusCode == 401) {
      throw const ApiException('Please sign in again.', statusCode: 401);
    }
    if (error.response?.statusCode == 403) {
      throw const ApiException('You are not allowed to do that.', statusCode: 403);
    }
    if (error.error is ApiException) {
      throw error.error as ApiException;
    }
    if (error.type == DioExceptionType.connectionError || error.type == DioExceptionType.connectionTimeout) {
      throw const ApiException(
        'Cannot reach the FixFlow API. Connect this phone to the same Wi-Fi as the PC and turn off mobile data.',
      );
    }
    throw ApiException(error.message ?? 'Request failed.');
  }
}
