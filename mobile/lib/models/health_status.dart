class HealthStatus {
  const HealthStatus({required this.status, required this.service});

  final String status;
  final String service;

  factory HealthStatus.fromJson(Map<String, dynamic> json) {
    return HealthStatus(
      status: json['status']?.toString() ?? 'unknown',
      service: json['service']?.toString() ?? 'FixFlow.Api',
    );
  }
}
