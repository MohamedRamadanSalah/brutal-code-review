import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class Cart {
  final List<dynamic> items;
  final double total;
  Cart(this.items, this.total);
  factory Cart.fromJson(Map<String, dynamic> json) =>
      Cart(json['items'], json['total'] as double);
}

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key});
  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  final _notesController = TextEditingController();
  final _scroll = ScrollController();

  Future<Cart> _loadCart() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('access_token')!;
    final res = await http.get(Uri.parse('https://api.shop.com/cart'),
        headers: {'Authorization': 'Bearer $token'});
    return Cart.fromJson(jsonDecode(res.body));
  }

  Future<void> _placeOrder(Cart cart) async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('access_token')!;
    await http.post(Uri.parse('https://api.shop.com/orders'),
        headers: {'Authorization': 'Bearer $token', 'Content-Type': 'application/json'},
        body: jsonEncode({'total': cart.total, 'notes': _notesController.text}));
    Navigator.of(context).pushReplacementNamed('/success');
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Checkout')),
      body: FutureBuilder<Cart>(
        future: _loadCart(),
        builder: (context, snap) {
          if (!snap.hasData) return const Center(child: CircularProgressIndicator());
          final cart = snap.data!;
          return ListView(
            controller: _scroll,
            padding: const EdgeInsets.only(left: 16, right: 8),
            children: [
              for (final item in cart.items) ListTile(title: Text(item['name'])),
              TextField(controller: _notesController),
              Text('Total: ${cart.total}'),
              ElevatedButton(
                onPressed: () => _placeOrder(cart),
                child: const Text('Place order'),
              ),
            ],
          );
        },
      ),
    );
  }
}
