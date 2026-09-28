import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/screens/admin/admin_home_screen.dart';
import 'package:fixflow_mobile/screens/admin/admin_technicians_screen.dart';
import 'package:fixflow_mobile/screens/admin/admin_verification_detail_screen.dart';
import 'package:fixflow_mobile/screens/admin/admin_verifications_screen.dart';
import 'package:fixflow_mobile/screens/auth/landing_home_screen.dart';
import 'package:fixflow_mobile/screens/auth/login_screen.dart';
import 'package:fixflow_mobile/screens/auth/register_screen.dart';
import 'package:fixflow_mobile/screens/auth/splash_screen.dart';
import 'package:fixflow_mobile/screens/auth/unauthorized_screen.dart';
import 'package:fixflow_mobile/screens/customer/booking_detail_screen.dart';
import 'package:fixflow_mobile/screens/customer/bookings_screen.dart';
import 'package:fixflow_mobile/screens/customer/confirm_booking_screen.dart';
import 'package:fixflow_mobile/screens/customer/create_request_screen.dart';
import 'package:fixflow_mobile/screens/customer/customer_home_screen.dart';
import 'package:fixflow_mobile/screens/customer/notifications_screen.dart';
import 'package:fixflow_mobile/screens/customer/quote_compare_screen.dart';
import 'package:fixflow_mobile/screens/customer/request_detail_screen.dart';
import 'package:fixflow_mobile/screens/customer/request_history_screen.dart';
import 'package:fixflow_mobile/screens/customer/review_screen.dart';
import 'package:fixflow_mobile/screens/technician/create_quote_screen.dart';
import 'package:fixflow_mobile/screens/technician/technician_home_screen.dart';
import 'package:fixflow_mobile/screens/technician/technician_invitations_screen.dart';
import 'package:fixflow_mobile/screens/technician/technician_jobs_screen.dart';
import 'package:fixflow_mobile/screens/technician/technician_profile_screen.dart';
import 'package:fixflow_mobile/screens/technician/technician_reviews_screen.dart';
import 'package:fixflow_mobile/screens/technician/technician_verification_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const ProviderScope(child: FixFlowApp()));
}

class FixFlowApp extends StatelessWidget {
  const FixFlowApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'FixFlow AI',
      theme: AppTheme.light(),
      initialRoute: AppRoutes.splash,
      onGenerateRoute: (settings) {
        final args = settings.arguments;
        switch (settings.name) {
          case AppRoutes.splash:
            return MaterialPageRoute(builder: (_) => const SplashScreen());
          case AppRoutes.home:
            return MaterialPageRoute(builder: (_) => const LandingHomeScreen());
          case AppRoutes.login:
            return MaterialPageRoute(builder: (_) => const LoginScreen());
          case AppRoutes.register:
            return MaterialPageRoute(
              builder: (_) => RegisterScreen(initialRole: args is String ? args : null),
            );
          case AppRoutes.unauthorized:
            return MaterialPageRoute(builder: (_) => const UnauthorizedScreen());
          case AppRoutes.customerHome:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.customerRoles, child: CustomerHomeScreen()),
            );
          case AppRoutes.createRequest:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.customerRoles, child: CreateRequestScreen()),
            );
          case AppRoutes.requestHistory:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.customerRoles, child: RequestHistoryScreen()),
            );
          case AppRoutes.requestDetail:
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.customerRoles,
                child: RequestDetailScreen(requestId: args as String),
              ),
            );
          case AppRoutes.quotes:
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.customerRoles,
                child: QuoteCompareScreen(request: args as ServiceRequest),
              ),
            );
          case AppRoutes.confirmBooking:
            final data = args as Map;
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.customerRoles,
                child: ConfirmBookingScreen(
                  request: data['request'] as ServiceRequest,
                  quote: data['quote'] as Quote,
                ),
              ),
            );
          case AppRoutes.bookings:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.customerRoles, child: BookingsScreen()),
            );
          case AppRoutes.bookingDetail:
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.customerRoles,
                child: BookingDetailScreen(bookingId: args as String),
              ),
            );
          case AppRoutes.review:
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.customerRoles,
                child: ReviewScreen(booking: args as Booking),
              ),
            );
          case AppRoutes.notifications:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.customerRoles, child: NotificationsScreen()),
            );
          case AppRoutes.technicianHome:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: TechnicianHomeScreen()),
            );
          case AppRoutes.technicianProfile:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: TechnicianProfileScreen()),
            );
          case AppRoutes.technicianVerification:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: TechnicianVerificationScreen()),
            );
          case AppRoutes.technicianInvitations:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: TechnicianInvitationsScreen()),
            );
          case AppRoutes.technicianQuote:
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.technicianRoles,
                child: CreateQuoteScreen(invitation: args as Invitation),
              ),
            );
          case AppRoutes.technicianJobs:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: TechnicianJobsScreen()),
            );
          case AppRoutes.technicianJobDetail:
            return MaterialPageRoute(
              builder: (_) => RoleGuard(
                roles: AppRoutes.technicianRoles,
                child: TechnicianJobDetailScreen(bookingId: args as String),
              ),
            );
          case AppRoutes.technicianReviews:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: TechnicianReviewsScreen()),
            );
          case AppRoutes.technicianNotifications:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.technicianRoles, child: NotificationsScreen()),
            );
          case AppRoutes.adminHome:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.adminRoles, child: AdminHomeScreen()),
            );
          case AppRoutes.adminTechnicians:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.adminRoles, child: AdminTechniciansScreen()),
            );
          case AppRoutes.adminVerifications:
            return MaterialPageRoute(
              builder: (_) => const RoleGuard(roles: AppRoutes.adminRoles, child: AdminVerificationsScreen()),
            );
          case AppRoutes.adminVerificationDetail:
            final id = settings.arguments as String? ?? '';
            return MaterialPageRoute(
              builder: (_) => RoleGuard(roles: AppRoutes.adminRoles, child: AdminVerificationDetailScreen(applicationId: id)),
            );
          default:
            return MaterialPageRoute(builder: (_) => const SplashScreen());
        }
      },
    );
  }
}
