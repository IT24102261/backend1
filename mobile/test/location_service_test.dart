import 'package:flutter_test/flutter_test.dart';
import 'package:fixflow_mobile/services/device_location_service.dart';

class _FakePlatform implements LocationPlatform {
  _FakePlatform({
    this.allowed = true,
    this.enabled = true,
    this.position,
    this.throwOnPosition = false,
  });

  final bool allowed;
  final bool enabled;
  final ({double latitude, double longitude})? position;
  final bool throwOnPosition;

  @override
  Future<bool> requestPermission() async => allowed;

  @override
  Future<bool> isServiceEnabled() async => enabled;

  @override
  Future<({double latitude, double longitude})> getCurrentPosition() async {
    if (throwOnPosition) {
      throw Exception('gps down');
    }
    return position ?? (latitude: 9.6615, longitude: 80.0255);
  }
}

void main() {
  test('GPS permission denied allows manual address and does not crash', () async {
    final service = DeviceLocationService(_FakePlatform(allowed: false));
    final result = await service.tryGetCurrentLocation();

    expect(result.permissionDenied, isTrue);
    expect(result.hasCoordinates, isFalse);
    expect(result.latitude, isNull);
    expect(result.message, contains('manually'));
  });

  test('GPS unavailable falls back to manual address', () async {
    final disabled = DeviceLocationService(_FakePlatform(enabled: false));
    final failed = DeviceLocationService(_FakePlatform(throwOnPosition: true));

    final off = await disabled.tryGetCurrentLocation();
    final error = await failed.tryGetCurrentLocation();

    expect(off.unavailable, isTrue);
    expect(off.hasCoordinates, isFalse);
    expect(error.unavailable, isTrue);
    expect(error.hasCoordinates, isFalse);
  });

  test('successful GPS returns coordinates', () async {
    final service = DeviceLocationService(_FakePlatform(position: (latitude: 9.66, longitude: 80.02)));
    final result = await service.tryGetCurrentLocation();

    expect(result.hasCoordinates, isTrue);
    expect(result.latitude, 9.66);
    expect(result.longitude, 80.02);
  });
}
