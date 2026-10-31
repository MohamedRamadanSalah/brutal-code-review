// Flutter client: computes the discount locally and sends it to the API.
Future<double> redeemCoupon(ApiClient api, Coupon coupon, Order order) async {
  final discount = coupon.amount; // coupon fetched earlier from GET /coupons/{code}
  final res = await api.post('/api/coupons/redeem', {
    'code': coupon.code,
    'orderId': order.id,
    'discountAmount': discount,
  });
  return (res['total'] as num).toDouble();
}
