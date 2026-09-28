import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';

class AuthenticatedMedia extends ConsumerStatefulWidget {
  const AuthenticatedMedia({
    super.key,
    required this.path,
    required this.label,
    this.mimeType,
    this.height = 220,
  });

  final String? path;
  final String label;
  final String? mimeType;
  final double height;

  @override
  ConsumerState<AuthenticatedMedia> createState() => _AuthenticatedMediaState();
}

class _AuthenticatedMediaState extends ConsumerState<AuthenticatedMedia> {
  Uint8List? _bytes;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant AuthenticatedMedia oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.path != widget.path) {
      _load();
    }
  }

  Future<void> _load() async {
    final path = widget.path;
    if (path == null || path.isEmpty) {
      setState(() {
        _loading = false;
        _bytes = null;
      });
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final bytes = await ref.read(apiProvider).documentBytes(path);
      if (!mounted) return;
      setState(() {
        _bytes = Uint8List.fromList(bytes);
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error.toString();
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return SizedBox(height: widget.height, child: const Center(child: CircularProgressIndicator()));
    }
    if (_error != null) {
      return Text(_error!, style: const TextStyle(color: Colors.red));
    }
    if (_bytes == null) {
      return Text('${widget.label} was not uploaded.', style: TextStyle(color: Colors.grey.shade700));
    }
    final pdf = (widget.mimeType ?? '').toLowerCase().contains('pdf');
    if (pdf) {
      return Container(
        height: 72,
        alignment: Alignment.centerLeft,
        padding: const EdgeInsets.all(12),
        color: const Color(0xFFF4EFE6),
        child: Text('${widget.label} (PDF uploaded). Open on web admin to inspect the file.'),
      );
    }
    return ClipRRect(
      borderRadius: BorderRadius.circular(16),
      child: Image.memory(_bytes!, height: widget.height, fit: BoxFit.contain),
    );
  }
}
