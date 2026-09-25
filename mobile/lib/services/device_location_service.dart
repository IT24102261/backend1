class DeviceLocationResult {
  const DeviceLocationResult({
    required this.message,
    this.latitude,
    this.longitude,
    this.permissionDenied = false,
    this.unavailable = false,
  });

  final double? latitude;
  final double? longitude;
  final bool permissionDenied;
  final bool unavailable;
  final String message;

  bool get hasCoordinates => latitude != null && longitude != null;
}

abstract class LocationPlatform {
  Future<bool> requestPermission();
  Future<bool> isServiceEnabled();
  Future<({double latitude, double longitude})> getCurrentPosition();
}

class DeviceLocationService {
  DeviceLocationService(this._platform);

  final LocationPlatform _platform;

  Future<DeviceLocationResult> tryGetCurrentLocation() async {
    try {
      final allowed = await _platform.requestPermission();
      if (!allowed) {
        return const DeviceLocationResult(
          permissionDenied: true,
          message: 'Location denied. Enter the address manually.',
        );
      }

      final enabled = await _platform.isServiceEnabled();
      if (!enabled) {
        return const DeviceLocationResult(
          unavailable: true,
          message: 'GPS is unavailable. Enter the address manually.',
        );
      }

      final position = await _platform.getCurrentPosition();
      return DeviceLocationResult(
        latitude: position.latitude,
        longitude: position.longitude,
        message:
            'GPS attached: ${position.latitude.toStringAsFixed(4)}, ${position.longitude.toStringAsFixed(4)}',
      );
    } catch (_) {
      return const DeviceLocationResult(
        unavailable: true,
        message: 'GPS is unavailable. Enter the address manually.',
      );
    }
  }
}
