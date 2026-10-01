import 'package:dio/dio.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/services/api_client.dart';

class FixFlowApi {
  FixFlowApi(this._client);

  final ApiClient _client;

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (error) {
      _client.throwFrom(error);
    }
  }

  Future<AuthSession> login(String email, String password) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/auth/login',
          data: {'email': email, 'password': password},
        );
        return AuthSession.fromJson(response.data ?? {});
      });

  Future<AuthSession> register({
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
  }) =>
      _guard(() async {
        if (role != 'TECHNICIAN') {
          final response = await _client.http.post<Map<String, dynamic>>(
            '/api/auth/register',
            data: {
              'email': email,
              'password': password,
              'displayName': displayName,
              'role': role,
              'phone': phone,
            },
          );
          return AuthSession.fromJson(response.data ?? {});
        }

        final form = FormData.fromMap({
          'email': email,
          'password': password,
          'displayName': displayName,
          'role': role,
          'phone': phone,
          'address': address,
          'categoryId': categoryId,
          if (nicPhotoPath != null)
            'nicPhoto': await MultipartFile.fromFile(nicPhotoPath, filename: nicPhotoName ?? 'nic.jpg'),
          if (certificatePath != null)
            'certificate': await MultipartFile.fromFile(certificatePath, filename: certificateName ?? 'certificate.pdf'),
          if (profilePhotoPath != null)
            'profilePhoto': await MultipartFile.fromFile(profilePhotoPath, filename: profilePhotoName ?? 'profile.jpg'),
        });
        final response = await _client.http.post<Map<String, dynamic>>('/api/auth/register/technician', data: form);
        return AuthSession.fromJson(response.data ?? {});
      });

  Future<void> logout(String refreshToken) => _guard(() async {
        await _client.http.post('/api/auth/logout', data: {'refreshToken': refreshToken});
      });

  Future<UserProfile> me() => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>('/api/me');
        return UserProfile.fromJson(response.data ?? {});
      });

  Future<List<Category>> categories() => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/categories');
        return (response.data ?? [])
            .whereType<Map<String, dynamic>>()
            .map(Category.fromJson)
            .toList();
      });

  Future<PagedResult<ServiceRequest>> requests({int page = 1, String? status}) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>(
          '/api/requests',
          queryParameters: {'page': page, 'pageSize': 20, 'status': ?status},
        );
        return PagedResult.fromJson(response.data ?? {}, ServiceRequest.fromJson);
      });

  Future<ServiceRequest> getRequest(String id) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>('/api/requests/$id');
        return ServiceRequest.fromJson(response.data ?? {});
      });

  Future<ServiceRequest> createRequest(Map<String, dynamic> payload) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>('/api/requests', data: payload);
        return ServiceRequest.fromJson(response.data ?? {});
      });

  Future<ServiceRequest> submitRequest(String id) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>('/api/requests/$id/submit');
        return ServiceRequest.fromJson(response.data ?? {});
      });

  Future<ServiceRequest> cancelRequest(String id) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>('/api/requests/$id/cancel');
        return ServiceRequest.fromJson(response.data ?? {});
      });

  Future<void> addClarification(String id, String message) => _guard(() async {
        await _client.http.post('/api/requests/$id/clarification', data: {'message': message});
      });

  Future<void> addMedia(String id, String path, String fileName) => _guard(() async {
        final form = FormData.fromMap({
          'file': await MultipartFile.fromFile(path, filename: fileName),
        });
        await _client.http.post('/api/requests/$id/media', data: form);
      });

  Future<List<RequestHistory>> requestHistory(String id) => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/requests/$id/history');
        return (response.data ?? [])
            .whereType<Map<String, dynamic>>()
            .map(RequestHistory.fromJson)
            .toList();
      });

  Future<List<Quote>> quotes(String requestId) => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/requests/$requestId/quotes');
        return (response.data ?? []).whereType<Map<String, dynamic>>().map(Quote.fromJson).toList();
      });

  Future<Quote> getQuote(String id) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>('/api/quotes/$id');
        return Quote.fromJson(response.data ?? {});
      });

  Future<Booking> selectQuote(String id) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>('/api/quotes/$id/select');
        return Booking.fromJson(response.data ?? {});
      });

  Future<Booking> confirmBooking(String bookingId) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/bookings/confirm',
          data: {'bookingId': bookingId},
        );
        return Booking.fromJson(response.data ?? {});
      });

  Future<PagedResult<Booking>> bookings({int page = 1, String? status}) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>(
          '/api/bookings',
          queryParameters: {'page': page, 'pageSize': 20, 'status': ?status},
        );
        return PagedResult.fromJson(response.data ?? {}, Booking.fromJson);
      });

  Future<Booking> getBooking(String id) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>('/api/bookings/$id');
        return Booking.fromJson(response.data ?? {});
      });

  Future<Booking> updateBookingStatus(String id, String status, {String? note}) => _guard(() async {
        final response = await _client.http.patch<Map<String, dynamic>>(
          '/api/bookings/$id/status',
          data: {'status': status, 'note': note},
        );
        return Booking.fromJson(response.data ?? {});
      });

  Future<Review> createReview(String bookingId, int rating, String? body) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/bookings/$bookingId/reviews',
          data: {'rating': rating, 'body': body},
        );
        return Review.fromJson(response.data ?? {});
      });

  Future<List<Review>> technicianReviews(String technicianId) => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/technicians/$technicianId/reviews');
        return (response.data ?? []).whereType<Map<String, dynamic>>().map(Review.fromJson).toList();
      });

  Future<List<AppNotification>> notifications() => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/notifications');
        return (response.data ?? []).whereType<Map<String, dynamic>>().map(AppNotification.fromJson).toList();
      });

  Future<void> markNotificationRead(String id) => _guard(() async {
        await _client.http.post('/api/notifications/$id/read');
      });

  Future<void> createComplaint({required String bookingId, required String subject, required String description}) =>
      _guard(() async {
        await _client.http.post(
          '/api/complaints',
          data: {'bookingId': bookingId, 'subject': subject, 'description': description},
        );
      });

  Future<TechnicianProfile> technicianProfile() => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>('/api/technicians/profile');
        return TechnicianProfile.fromJson(response.data ?? {});
      });

  Future<TechnicianProfile> saveTechnicianProfile(Map<String, dynamic> payload, {required bool exists}) =>
      _guard(() async {
        final response = exists
            ? await _client.http.put<Map<String, dynamic>>('/api/technicians/profile', data: payload)
            : await _client.http.post<Map<String, dynamic>>('/api/technicians/profile', data: payload);
        return TechnicianProfile.fromJson(response.data ?? {});
      });

  Future<TechnicianApplication> applyCategory(String categoryId) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/technician-applications',
          data: {'categoryId': categoryId},
        );
        return TechnicianApplication.fromJson(response.data ?? {});
      });

  Future<List<TechnicianApplication>> myApplications() => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/technician-applications');
        return (response.data ?? [])
            .whereType<Map<String, dynamic>>()
            .map(TechnicianApplication.fromJson)
            .toList();
      });

  Future<void> uploadDocument(String applicationId, String path, String fileName, String evidenceType) =>
      _guard(() async {
        final form = FormData.fromMap({
          'file': await MultipartFile.fromFile(path, filename: fileName),
          'evidenceType': evidenceType,
        });
        await _client.http.post('/api/technician-applications/$applicationId/documents', data: form);
      });

  Future<List<Invitation>> invitations() => _guard(() async {
        final response = await _client.http.get<List<dynamic>>('/api/invitations');
        return (response.data ?? []).whereType<Map<String, dynamic>>().map(Invitation.fromJson).toList();
      });

  Future<Invitation> acceptInvitation(String id) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>('/api/invitations/$id/accept');
        return Invitation.fromJson(response.data ?? {});
      });

  Future<Invitation> declineInvitation(String id) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>('/api/invitations/$id/decline');
        return Invitation.fromJson(response.data ?? {});
      });

  Future<Quote> createQuote(String invitationId, Map<String, dynamic> payload) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/invitations/$invitationId/quote',
          data: payload,
        );
        return Quote.fromJson(response.data ?? {});
      });

  Future<Map<String, dynamic>> reverseGeocode(double latitude, double longitude) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/maps/reverse',
          data: {'latitude': latitude, 'longitude': longitude},
        );
        return response.data ?? {};
      });

  Future<PagedResult<TechnicianApplication>> adminApplications({int page = 1, String status = ''}) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>(
          '/api/admin/technician-applications',
          queryParameters: {'page': page, 'pageSize': 50, if (status.isNotEmpty) 'status': status},
        );
        return PagedResult.fromJson(response.data ?? {}, TechnicianApplication.fromJson);
      });

  Future<TechnicianApplication> adminApplication(String id) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>('/api/admin/technician-applications/$id');
        return TechnicianApplication.fromJson(response.data ?? {});
      });

  Future<TechnicianApplication> decideApplication(String id, String action, String notes) => _guard(() async {
        final response = await _client.http.post<Map<String, dynamic>>(
          '/api/admin/technician-applications/$id/$action',
          data: {'notes': notes},
        );
        return TechnicianApplication.fromJson(response.data ?? {});
      });

  Future<List<int>> documentBytes(String path) => _guard(() async {
        final response = await _client.http.get<List<int>>(path, options: Options(responseType: ResponseType.bytes));
        return response.data ?? <int>[];
      });

  Future<PagedResult<TechnicianReport>> technicianReports({int page = 1}) => _guard(() async {
        final response = await _client.http.get<Map<String, dynamic>>(
          '/api/admin/reports/technicians',
          queryParameters: {'page': page, 'pageSize': 50},
        );
        return PagedResult.fromJson(response.data ?? {}, TechnicianReport.fromJson);
      });

  Future<TechnicianProfile> setOwnProfilePhoto(String path, String fileName) => _guard(() async {
        final form = FormData.fromMap({
          'file': await MultipartFile.fromFile(path, filename: fileName),
        });
        final response = await _client.http.post<Map<String, dynamic>>('/api/technicians/profile/photo', data: form);
        return TechnicianProfile.fromJson(response.data ?? {});
      });

  Future<void> setTechnicianPhoto(String technicianId, String path, String fileName) => _guard(() async {
        final form = FormData.fromMap({
          'file': await MultipartFile.fromFile(path, filename: fileName),
        });
        await _client.http.post('/api/admin/technicians/$technicianId/photo', data: form);
      });

  Future<Workflow?> tryWorkflow(String id) async {
    try {
      final response = await _client.http.get<Map<String, dynamic>>('/api/ai/workflows/$id');
      return Workflow.fromJson(response.data ?? {});
    } catch (_) {
      return null;
    }
  }
}
