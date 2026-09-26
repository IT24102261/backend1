DateTime? _dt(dynamic value) => value == null ? null : DateTime.tryParse(value.toString());

List<String> _stringList(dynamic value) {
  if (value is! List) return const [];
  return value.map((item) => item.toString().trim()).where((item) => item.isNotEmpty).toList();
}

class PagedResult<T> {
  const PagedResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;

  factory PagedResult.fromJson(Map<String, dynamic> json, T Function(Map<String, dynamic>) map) {
    final raw = json['items'] as List<dynamic>? ?? [];
    return PagedResult(
      items: raw.whereType<Map<String, dynamic>>().map(map).toList(),
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 20,
      totalCount: json['totalCount'] as int? ?? raw.length,
    );
  }
}

class AuthSession {
  const AuthSession({
    required this.userId,
    required this.accessToken,
    required this.refreshToken,
    required this.email,
    required this.role,
    this.requiresAdminApproval = false,
  });

  final String userId;
  final String accessToken;
  final String refreshToken;
  final String email;
  final String role;
  final bool requiresAdminApproval;

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        userId: json['userId']?.toString() ?? '',
        accessToken: json['accessToken']?.toString() ?? '',
        refreshToken: json['refreshToken']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        role: json['role']?.toString() ?? '',
        requiresAdminApproval: json['requiresAdminApproval'] as bool? ?? false,
      );
}

class UserProfile {
  const UserProfile({
    required this.userId,
    required this.email,
    required this.displayName,
    required this.role,
    this.phone,
    this.technicianProfileId,
    this.approvedCategories = const [],
  });

  final String userId;
  final String email;
  final String displayName;
  final String role;
  final String? phone;
  final String? technicianProfileId;
  final List<String> approvedCategories;

  bool get isCustomer => role == 'CUSTOMER';
  bool get isTechnician => role == 'TECHNICIAN';
  bool get isAdmin => role == 'ADMIN';

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
        userId: json['userId']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        displayName: json['displayName']?.toString() ?? '',
        role: json['role']?.toString() ?? '',
        phone: json['phone']?.toString(),
        technicianProfileId: json['technicianProfileId']?.toString(),
        approvedCategories: (json['approvedCategories'] as List<dynamic>? ?? [])
            .map((item) => item.toString())
            .toList(),
      );

  Map<String, dynamic> toJson() => {
        'userId': userId,
        'email': email,
        'displayName': displayName,
        'role': role,
        'phone': phone,
        'technicianProfileId': technicianProfileId,
        'approvedCategories': approvedCategories,
      };
}

class Category {
  const Category({required this.id, required this.name, this.description, this.isActive = true});

  final String id;
  final String name;
  final String? description;
  final bool isActive;

  factory Category.fromJson(Map<String, dynamic> json) => Category(
        id: json['id']?.toString() ?? '',
        name: json['name']?.toString() ?? '',
        description: json['description']?.toString(),
        isActive: json['isActive'] as bool? ?? true,
      );
}

class ServiceRequest {
  const ServiceRequest({
    required this.id,
    required this.customerId,
    required this.description,
    required this.status,
    required this.createdAt,
    this.categoryId,
    this.categoryName,
    this.serviceArea,
    this.address,
    this.preferredStart,
    this.preferredEnd,
  });

  final String id;
  final String customerId;
  final String description;
  final String status;
  final DateTime createdAt;
  final String? categoryId;
  final String? categoryName;
  final String? serviceArea;
  final String? address;
  final DateTime? preferredStart;
  final DateTime? preferredEnd;

  factory ServiceRequest.fromJson(Map<String, dynamic> json) => ServiceRequest(
        id: json['id']?.toString() ?? '',
        customerId: json['customerId']?.toString() ?? '',
        description: json['description']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
        createdAt: _dt(json['createdAt']) ?? DateTime.now(),
        categoryId: json['categoryId']?.toString(),
        categoryName: json['categoryName']?.toString(),
        serviceArea: json['serviceArea']?.toString(),
        address: json['address']?.toString(),
        preferredStart: _dt(json['preferredStart']),
        preferredEnd: _dt(json['preferredEnd']),
      );
}

class RequestHistory {
  const RequestHistory({
    required this.id,
    required this.fromStatus,
    required this.toStatus,
    required this.timestamp,
    this.note,
  });

  final String id;
  final String fromStatus;
  final String toStatus;
  final DateTime timestamp;
  final String? note;

  factory RequestHistory.fromJson(Map<String, dynamic> json) => RequestHistory(
        id: json['id']?.toString() ?? '',
        fromStatus: json['fromStatus']?.toString() ?? '',
        toStatus: json['toStatus']?.toString() ?? '',
        timestamp: _dt(json['timestamp']) ?? DateTime.now(),
        note: json['note']?.toString(),
      );
}

class Quote {
  const Quote({
    required this.id,
    required this.requestId,
    required this.technicianId,
    required this.labourAmount,
    required this.materialsAmount,
    required this.travelAmount,
    required this.totalAmount,
    required this.currency,
    required this.durationMinutes,
    required this.expiresAt,
    required this.status,
    required this.version,
    this.arrivalStart,
    this.assumptions,
    this.approximateDistanceKm,
    this.distanceUnavailable = false,
    this.distanceBand,
    this.technicianDisplayName,
    this.profilePhotoUrl,
    this.recommendationSummary,
    this.strengths = const [],
    this.tradeoffs = const [],
  });

  final String id;
  final String requestId;
  final String technicianId;
  final num labourAmount;
  final num materialsAmount;
  final num travelAmount;
  final num totalAmount;
  final String currency;
  final int durationMinutes;
  final DateTime expiresAt;
  final String status;
  final int version;
  final DateTime? arrivalStart;
  final String? assumptions;
  final num? approximateDistanceKm;
  final bool distanceUnavailable;
  final String? distanceBand;
  final String? technicianDisplayName;
  final String? profilePhotoUrl;
  final String? recommendationSummary;
  final List<String> strengths;
  final List<String> tradeoffs;

  factory Quote.fromJson(Map<String, dynamic> json) => Quote(
        id: json['id']?.toString() ?? '',
        requestId: json['requestId']?.toString() ?? '',
        technicianId: json['technicianId']?.toString() ?? '',
        labourAmount: json['labourAmount'] as num? ?? 0,
        materialsAmount: json['materialsAmount'] as num? ?? 0,
        travelAmount: json['travelAmount'] as num? ?? 0,
        totalAmount: json['totalAmount'] as num? ?? 0,
        currency: json['currency']?.toString() ?? 'LKR',
        durationMinutes: json['durationMinutes'] as int? ?? 0,
        expiresAt: _dt(json['expiresAt']) ?? DateTime.now(),
        status: json['status']?.toString() ?? '',
        version: json['version'] as int? ?? 1,
        arrivalStart: _dt(json['arrivalStart']),
        assumptions: json['assumptions']?.toString(),
        approximateDistanceKm: json['approximateDistanceKm'] as num?,
        distanceUnavailable: json['distanceUnavailable'] as bool? ?? false,
        distanceBand: json['distanceBand']?.toString(),
        technicianDisplayName: json['technicianDisplayName']?.toString(),
        profilePhotoUrl: json['profilePhotoUrl']?.toString(),
        recommendationSummary: json['recommendationSummary']?.toString(),
        strengths: _stringList(json['strengths']),
        tradeoffs: _stringList(json['tradeoffs']),
      );
}

class Booking {
  const Booking({
    required this.id,
    required this.requestId,
    required this.quotationId,
    required this.customerId,
    required this.technicianId,
    required this.status,
    this.confirmedAt,
    this.address,
    this.latitude,
    this.longitude,
    this.customerDisplayName,
    this.technicianDisplayName,
    this.customerPhone,
    this.categoryName,
    this.requestDescription,
    this.serviceArea,
    this.preferredStart,
    this.quoteTotalAmount,
    this.currency,
    this.profilePhotoUrl,
  });

  final String id;
  final String requestId;
  final String quotationId;
  final String customerId;
  final String technicianId;
  final String status;
  final DateTime? confirmedAt;
  final String? address;
  final double? latitude;
  final double? longitude;
  final String? customerDisplayName;
  final String? technicianDisplayName;
  final String? customerPhone;
  final String? categoryName;
  final String? requestDescription;
  final String? serviceArea;
  final DateTime? preferredStart;
  final double? quoteTotalAmount;
  final String? currency;
  final String? profilePhotoUrl;

  bool get hasExactLocation =>
      address != null && address!.isNotEmpty || latitude != null && longitude != null;

  factory Booking.fromJson(Map<String, dynamic> json) => Booking(
        id: json['id']?.toString() ?? '',
        requestId: json['requestId']?.toString() ?? '',
        quotationId: json['quotationId']?.toString() ?? '',
        customerId: json['customerId']?.toString() ?? '',
        technicianId: json['technicianId']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
        confirmedAt: _dt(json['confirmedAt']),
        address: json['address']?.toString(),
        latitude: (json['latitude'] as num?)?.toDouble(),
        longitude: (json['longitude'] as num?)?.toDouble(),
        customerDisplayName: json['customerDisplayName']?.toString(),
        technicianDisplayName: json['technicianDisplayName']?.toString(),
        customerPhone: json['customerPhone']?.toString(),
        categoryName: json['categoryName']?.toString(),
        requestDescription: json['requestDescription']?.toString(),
        serviceArea: json['serviceArea']?.toString(),
        preferredStart: _dt(json['preferredStart']),
        quoteTotalAmount: (json['quoteTotalAmount'] as num?)?.toDouble(),
        currency: json['currency']?.toString(),
        profilePhotoUrl: json['profilePhotoUrl']?.toString(),
      );
}

class Review {
  const Review({
    required this.id,
    required this.bookingId,
    required this.technicianId,
    required this.rating,
    required this.status,
    this.body,
    this.customerDisplayName,
    this.technicianDisplayName,
    this.adminReply,
  });

  final String id;
  final String bookingId;
  final String technicianId;
  final int rating;
  final String status;
  final String? body;
  final String? customerDisplayName;
  final String? technicianDisplayName;
  final String? adminReply;

  factory Review.fromJson(Map<String, dynamic> json) => Review(
        id: json['id']?.toString() ?? '',
        bookingId: json['bookingId']?.toString() ?? '',
        technicianId: json['technicianId']?.toString() ?? '',
        rating: json['rating'] as int? ?? 0,
        status: json['status']?.toString() ?? '',
        body: json['body']?.toString(),
        customerDisplayName: json['customerDisplayName']?.toString(),
        technicianDisplayName: json['technicianDisplayName']?.toString(),
        adminReply: json['adminReply']?.toString(),
      );
}

class AppNotification {
  const AppNotification({
    required this.id,
    required this.title,
    required this.message,
    required this.isRead,
    this.createdAt,
  });

  final String id;
  final String title;
  final String message;
  final bool isRead;
  final DateTime? createdAt;

  factory AppNotification.fromJson(Map<String, dynamic> json) => AppNotification(
        id: json['id']?.toString() ?? '',
        title: json['title']?.toString() ?? '',
        message: json['message']?.toString() ?? '',
        isRead: json['isRead'] == true,
        createdAt: _dt(json['createdAt']),
      );
}

class Invitation {
  const Invitation({
    required this.id,
    required this.requestId,
    required this.status,
    required this.sentAt,
    required this.description,
    this.serviceArea,
    this.categoryName,
    this.requestStatus,
    this.canQuote = true,
  });

  final String id;
  final String requestId;
  final String status;
  final DateTime sentAt;
  final String description;
  final String? serviceArea;
  final String? categoryName;
  final String? requestStatus;
  final bool canQuote;

  factory Invitation.fromJson(Map<String, dynamic> json) => Invitation(
        id: json['id']?.toString() ?? '',
        requestId: json['requestId']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
        sentAt: _dt(json['sentAt']) ?? DateTime.now(),
        description: json['description']?.toString() ?? '',
        serviceArea: json['serviceArea']?.toString(),
        categoryName: json['categoryName']?.toString(),
        requestStatus: json['requestStatus']?.toString(),
        canQuote: json['canQuote'] as bool? ?? true,
      );
}

class TechnicianProfile {
  const TechnicianProfile({
    required this.id,
    required this.userId,
    required this.displayName,
    required this.averageRating,
    required this.reviewCount,
    required this.approvedCategories,
    this.bio,
    this.serviceArea,
    this.experienceSummary,
    this.isSuspended = false,
  });

  final String id;
  final String userId;
  final String displayName;
  final num averageRating;
  final int reviewCount;
  final List<String> approvedCategories;
  final String? bio;
  final String? serviceArea;
  final String? experienceSummary;
  final bool isSuspended;

  factory TechnicianProfile.fromJson(Map<String, dynamic> json) => TechnicianProfile(
        id: json['id']?.toString() ?? '',
        userId: json['userId']?.toString() ?? '',
        displayName: json['displayName']?.toString() ?? '',
        averageRating: json['averageRating'] as num? ?? 0,
        reviewCount: json['reviewCount'] as int? ?? 0,
        approvedCategories: (json['approvedCategories'] as List<dynamic>? ?? [])
            .map((item) => item.toString())
            .toList(),
        bio: json['bio']?.toString(),
        serviceArea: json['serviceArea']?.toString(),
        experienceSummary: json['experienceSummary']?.toString(),
        isSuspended: json['isSuspended'] as bool? ?? false,
      );
}

class TechnicianApplication {
  const TechnicianApplication({
    required this.id,
    required this.categoryId,
    required this.status,
    required this.submittedAt,
    this.categoryName,
    this.decisionNotes,
  });

  final String id;
  final String categoryId;
  final String status;
  final DateTime submittedAt;
  final String? categoryName;
  final String? decisionNotes;

  factory TechnicianApplication.fromJson(Map<String, dynamic> json) => TechnicianApplication(
        id: json['id']?.toString() ?? '',
        categoryId: json['categoryId']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
        submittedAt: _dt(json['submittedAt']) ?? DateTime.now(),
        categoryName: json['categoryName']?.toString(),
        decisionNotes: json['decisionNotes']?.toString(),
      );
}

class Workflow {
  const Workflow({
    required this.id,
    required this.requestId,
    required this.objective,
    required this.status,
    required this.approvalStatus,
    required this.currentStep,
    this.planJson,
  });

  final String id;
  final String requestId;
  final String objective;
  final String status;
  final String approvalStatus;
  final String currentStep;
  final String? planJson;

  factory Workflow.fromJson(Map<String, dynamic> json) => Workflow(
        id: json['id']?.toString() ?? '',
        requestId: json['requestId']?.toString() ?? '',
        objective: json['objective']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
        approvalStatus: json['approvalStatus']?.toString() ?? '',
        currentStep: json['currentStep']?.toString() ?? '',
        planJson: json['planJson']?.toString(),
      );
}
