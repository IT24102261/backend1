import 'package:geolocator/geolocator.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:fixflow_mobile/services/device_location_service.dart';

class GeolocatorLocationPlatform implements LocationPlatform {
  @override
  Future<bool> requestPermission() async {
    final status = await Permission.locationWhenInUse.request();
    if (status.isGranted) {
      return true;
    }

    final geo = await Geolocator.requestPermission();
    return geo == LocationPermission.always || geo == LocationPermission.whileInUse;
  }

  @override
  Future<bool> isServiceEnabled() => Geolocator.isLocationServiceEnabled();

  @override
  Future<({double latitude, double longitude})> getCurrentPosition() async {
    final position = await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(accuracy: LocationAccuracy.medium, timeLimit: Duration(seconds: 12)),
    );
    return (latitude: position.latitude, longitude: position.longitude);
  }
}
