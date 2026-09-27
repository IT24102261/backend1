import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/screens/auth/unauthorized_screen.dart';

class AppRoutes {
  static const splash = '/';
  static const home = '/home';
  static const login = '/login';
  static const register = '/register';
  static const unauthorized = '/unauthorized';
  static const customerHome = '/customer';
  static const createRequest = '/customer/requests/new';
  static const requestHistory = '/customer/requests';
  static const requestDetail = '/customer/requests/detail';
  static const quotes = '/customer/quotes';
  static const confirmBooking = '/customer/booking/confirm';
  static const bookings = '/customer/bookings';
  static const bookingDetail = '/customer/bookings/detail';
  static const review = '/customer/review';
  static const notifications = '/customer/notifications';
  static const technicianHome = '/technician';
  static const technicianProfile = '/technician/profile';
  static const technicianVerification = '/technician/verification';
  static const technicianInvitations = '/technician/invitations';
  static const technicianQuote = '/technician/quote';
  static const technicianJobs = '/technician/jobs';
  static const technicianJobDetail = '/technician/jobs/detail';
  static const technicianReviews = '/technician/reviews';
  static const technicianNotifications = '/technician/notifications';
  static const adminHome = '/admin';
  static const adminTechnicians = '/admin/technicians';

  static const customerRoles = ['CUSTOMER'];
  static const technicianRoles = ['TECHNICIAN'];
  static const adminRoles = ['ADMIN'];
}

class RoleGuard extends ConsumerWidget {
  const RoleGuard({super.key, required this.roles, required this.child});

  final List<String> roles;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    return auth.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (_, _) => const UnauthorizedScreen(),
      data: (user) {
        if (user == null) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (context.mounted) {
              Navigator.pushNamedAndRemoveUntil(context, AppRoutes.login, (_) => false);
            }
          });
          return const Scaffold(body: Center(child: CircularProgressIndicator()));
        }
        if (!roles.contains(user.role)) {
          return const UnauthorizedScreen();
        }
        return child;
      },
    );
  }
}