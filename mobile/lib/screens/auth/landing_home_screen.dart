import 'package:flutter/material.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class LandingHomeScreen extends StatelessWidget {
  const LandingHomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        titleSpacing: 16,
        title: Row(
          children: [
            InkWell(
              onTap: () {
                if (ModalRoute.of(context)?.settings.name == AppRoutes.home) return;
                Navigator.pushNamedAndRemoveUntil(context, AppRoutes.home, (_) => false);
              },
              borderRadius: BorderRadius.circular(10),
              child: const BrandMark(size: 32),
            ),
            const SizedBox(width: 10),
            const Text('FixFlow AI', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: AppColors.ink)),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pushNamed(context, AppRoutes.login),
            child: const Text('Login'),
          ),
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: FilledButton(
              onPressed: () => Navigator.pushNamed(context, AppRoutes.register),
              style: FilledButton.styleFrom(minimumSize: const Size(0, 40), padding: const EdgeInsets.symmetric(horizontal: 14)),
              child: const Text('Get started'),
            ),
          ),
        ],
      ),
      body: ListView(
        children: [
          Container(
            margin: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            padding: const EdgeInsets.fromLTRB(20, 24, 20, 24),
            decoration: BoxDecoration(
              color: AppColors.cream,
              borderRadius: BorderRadius.circular(28),
              border: Border.all(color: AppColors.cardBorder),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Building trust, one home at a time', style: TextStyle(color: AppColors.muted, fontSize: 12, fontWeight: FontWeight.w600)),
                const SizedBox(height: 12),
                Text('Trusted home services. Lasting value.', style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 12),
                const Text(
                  'FixFlow AI connects customers with verified technicians, compares real quotations, and keeps the final booking decision with you.',
                  style: TextStyle(color: AppColors.muted, height: 1.5),
                ),
                const SizedBox(height: 20),
                FilledButton(
                  onPressed: () => Navigator.pushNamed(context, AppRoutes.register, arguments: 'CUSTOMER'),
                  child: const Text('Find a technician'),
                ),
                const SizedBox(height: 8),
                OutlinedButton(
                  onPressed: () => Navigator.pushNamed(context, AppRoutes.login),
                  child: const Text('Sign in'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          _SectionTitle(kicker: 'Services', title: 'Trades we match'),
          const _ServiceGrid(),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(24),
                border: Border.all(color: AppColors.cardBorder),
              ),
              child: const Column(
                children: [
                  _StatRow(icon: Icons.verified_outlined, title: 'Verified technicians', detail: 'Category approval before any invitation is sent.'),
                  SizedBox(height: 16),
                  _StatRow(icon: Icons.request_quote_outlined, title: 'Real quotations', detail: 'Compare price, arrival, distance and reputation.'),
                  SizedBox(height: 16),
                  _StatRow(icon: Icons.thumb_up_outlined, title: 'Customer approval', detail: 'AI never books a technician without your confirm step.'),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          _SectionTitle(kicker: 'How it works', title: 'Our latest workflow'),
          const _StepCard(step: '01', title: 'Request a service', body: 'Describe the problem, add a photo and your preferred time.'),
          const _StepCard(step: '02', title: 'Compare quotations', body: 'Approved technicians send real prices. We explain the differences so you can choose.'),
          const _StepCard(step: '03', title: 'Confirm the booking', body: 'You pick a quotation and confirm. We only check that it is still valid — we never book for you.'),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Container(
              padding: const EdgeInsets.all(22),
              decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(24)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('CUSTOMERS', style: TextStyle(color: AppColors.goldDark, fontSize: 11, fontWeight: FontWeight.w700, letterSpacing: 0.8)),
                  const SizedBox(height: 8),
                  Text('For homeowners', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 12),
                  const Text('Create requests, compare quotations, confirm the booking, and leave a review.'),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: () => Navigator.pushNamed(context, AppRoutes.register, arguments: 'CUSTOMER'),
                    child: const Text('Get started as customer'),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Container(
              padding: const EdgeInsets.all(22),
              decoration: BoxDecoration(color: AppColors.navy, borderRadius: BorderRadius.circular(24)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('TECHNICIANS', style: TextStyle(color: AppColors.gold, fontSize: 11, fontWeight: FontWeight.w700, letterSpacing: 0.8)),
                  const SizedBox(height: 8),
                  const Text('For tradespeople', style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.w600)),
                  const SizedBox(height: 12),
                  const Text(
                    'Apply per category, receive invitations in your approved trade, send quotes, and update the job.',
                    style: TextStyle(color: Color(0xB3FFFFFF), height: 1.5),
                  ),
                  const SizedBox(height: 16),
                  OutlinedButton(
                    onPressed: () => Navigator.pushNamed(context, AppRoutes.register, arguments: 'TECHNICIAN'),
                    style: OutlinedButton.styleFrom(foregroundColor: Colors.white, side: const BorderSide(color: Color(0x4DFFFFFF))),
                    child: const Text('Join as technician'),
                  ),
                ],
              ),
            ),
          ),
          Container(
            margin: const EdgeInsets.fromLTRB(16, 8, 16, 28),
            padding: const EdgeInsets.fromLTRB(22, 28, 22, 28),
            decoration: BoxDecoration(color: AppColors.navy, borderRadius: BorderRadius.circular(28)),
            child: Column(
              children: [
                const Text('Start a request', style: TextStyle(color: AppColors.gold, fontSize: 12, fontWeight: FontWeight.w700)),
                const SizedBox(height: 10),
                const Text(
                  'Ready when your home needs help',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 10),
                const Text(
                  'Create a FixFlow account and connect with eligible technicians through a safer, more transparent workflow.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Color(0xB3FFFFFF), height: 1.5),
                ),
                const SizedBox(height: 20),
                FilledButton(
                  onPressed: () => Navigator.pushNamed(context, AppRoutes.register, arguments: 'CUSTOMER'),
                  style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: AppColors.navy),
                  child: const Text('Create customer account'),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle({required this.kicker, required this.title});

  final String kicker;
  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 14),
      child: Column(
        children: [
          Text(kicker.toUpperCase(), style: const TextStyle(color: AppColors.goldDark, fontSize: 11, fontWeight: FontWeight.w700, letterSpacing: 0.8)),
          const SizedBox(height: 6),
          Text(title, style: Theme.of(context).textTheme.headlineSmall, textAlign: TextAlign.center),
        ],
      ),
    );
  }
}

class _ServiceGrid extends StatelessWidget {
  const _ServiceGrid();

  @override
  Widget build(BuildContext context) {
    const items = [
      (Icons.bolt_outlined, 'Electrical'),
      (Icons.plumbing_outlined, 'Plumbing'),
      (Icons.carpenter_outlined, 'Carpentry'),
      (Icons.ac_unit_outlined, 'AC / Refrigeration'),
      (Icons.wb_sunny_outlined, 'Solar'),
      (Icons.format_paint_outlined, 'Painter'),
    ];
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      child: Wrap(
        spacing: 10,
        runSpacing: 10,
        children: [
          for (final item in items)
            Container(
              width: (MediaQuery.sizeOf(context).width - 42) / 2,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppColors.navy,
                borderRadius: BorderRadius.circular(20),
              ),
              child: Column(
                children: [
                  Icon(item.$1, color: AppColors.gold),
                  const SizedBox(height: 10),
                  Text(item.$2, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _StatRow extends StatelessWidget {
  const _StatRow({required this.icon, required this.title, required this.detail});

  final IconData icon;
  final String title;
  final String detail;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 40,
          height: 40,
          decoration: BoxDecoration(shape: BoxShape.circle, border: Border.all(color: const Color(0x26171717))),
          child: Icon(icon, size: 18, color: AppColors.goldDark),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title.toUpperCase(), style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.ink)),
              const SizedBox(height: 4),
              Text(detail, style: const TextStyle(color: AppColors.muted, height: 1.45)),
            ],
          ),
        ),
      ],
    );
  }
}

class _StepCard extends StatelessWidget {
  const _StepCard({required this.step, required this.title, required this.body});

  final String step;
  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(24),
          border: Border.all(color: AppColors.cardBorder),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(step, style: const TextStyle(color: AppColors.goldDark, fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 6),
            Text(body, style: const TextStyle(color: AppColors.muted, height: 1.45)),
          ],
        ),
      ),
    );
  }
}
