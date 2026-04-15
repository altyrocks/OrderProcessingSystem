import { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  SafeAreaView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

type Order = {
  id: string;
  productName: string;
  quantity: number;
  status: string;
  createdAt: string;
};

const terminalStatuses = new Set(['Payment Succeeded', 'Inventory Released']);

export default function HomeScreen() {
  const [apiBaseUrl, setApiBaseUrl] = useState('https://localhost:7150');
  const [productName, setProductName] = useState('Laptop');
  const [quantity, setQuantity] = useState('1');
  const [orders, setOrders] = useState<Order[]>([]);
  const [loadingOrders, setLoadingOrders] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const pollerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    return () => {
      if (pollerRef.current) {
        clearInterval(pollerRef.current);
      }
    };
  }, []);

  const normalizeBaseUrl = () => apiBaseUrl.trim().replace(/\/+$/, '');

  const fetchOrders = async () => {
    setLoadingOrders(true);

    try {
      const response = await fetch(`${normalizeBaseUrl()}/api/orders`);

      if (!response.ok) {
        throw new Error(`API returned ${response.status}`);
      }

      const data = (await response.json()) as Order[];
      setOrders(data);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unknown error';
      Alert.alert('Could not load orders', message);
    } finally {
      setLoadingOrders(false);
    }
  };

  const startPolling = () => {
    if (pollerRef.current) {
      clearInterval(pollerRef.current);
    }

    pollerRef.current = setInterval(async () => {
      try {
        const response = await fetch(`${normalizeBaseUrl()}/api/orders`);

        if (!response.ok) {
          return;
        }

        const data = (await response.json()) as Order[];
        setOrders(data);

        const stillInFlight = data.some((order) => !terminalStatuses.has(order.status));

        if (!stillInFlight && pollerRef.current) {
          clearInterval(pollerRef.current);
          pollerRef.current = null;
        }
      } catch {
        if (pollerRef.current) {
          clearInterval(pollerRef.current);
          pollerRef.current = null;
        }
      }
    }, 2000);
  };

  const createOrder = async () => {
    const parsedQuantity = Number.parseInt(quantity, 10);

    if (!productName.trim()) {
      Alert.alert('Missing product', 'Enter a product name before creating an order.');
      return;
    }

    if (Number.isNaN(parsedQuantity) || parsedQuantity <= 0) {
      Alert.alert('Invalid quantity', 'Quantity must be a whole number greater than zero.');
      return;
    }

    setSubmitting(true);

    try {
      const response = await fetch(`${normalizeBaseUrl()}/api/orders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          productName: productName.trim(),
          quantity: parsedQuantity,
        }),
      });

      if (!response.ok) {
        throw new Error(`API returned ${response.status}`);
      }

      const createdOrder = (await response.json()) as Order;
      setOrders((currentOrders) => [createdOrder, ...currentOrders]);
      startPolling();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unknown error';
      Alert.alert('Could not create order', message);
    } finally {
      setSubmitting(false);
    }
  };

  const renderOrder = ({ item }: { item: Order }) => (
    <View style={styles.orderCard}>
      <View style={styles.orderHeader}>
        <Text style={styles.orderProduct}>{item.productName}</Text>
        <Text style={styles.orderStatus}>{item.status}</Text>
      </View>
      <Text style={styles.orderMeta}>Quantity: {item.quantity}</Text>
      <Text style={styles.orderMeta}>Order Id: {item.id}</Text>
      <Text style={styles.orderMeta}>
        Created: {new Date(item.createdAt).toLocaleString()}
      </Text>
    </View>
  );

  return (
    <SafeAreaView style={styles.safeArea}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.flex}>
        <FlatList
          data={orders}
          keyExtractor={(item) => item.id}
          renderItem={renderOrder}
          contentContainerStyle={styles.content}
          ListHeaderComponent={
            <View style={styles.headerSection}>
              <Text style={styles.title}>Order Control Panel</Text>
              <Text style={styles.subtitle}>
                This screen talks to your API, creates orders, and keeps refreshing so you can
                watch the saga move through each status.
              </Text>

              <View style={styles.panel}>
                <Text style={styles.label}>API base URL</Text>
                <TextInput
                  autoCapitalize="none"
                  autoCorrect={false}
                  onChangeText={setApiBaseUrl}
                  style={styles.input}
                  value={apiBaseUrl}
                />
                <Text style={styles.helpText}>
                  Use the URL where Orders.Api is running. On a phone, localhost usually means the
                  phone itself, not your computer.
                </Text>
              </View>

              <View style={styles.panel}>
                <Text style={styles.label}>Product name</Text>
                <TextInput onChangeText={setProductName} style={styles.input} value={productName} />

                <Text style={styles.label}>Quantity</Text>
                <TextInput
                  keyboardType="number-pad"
                  onChangeText={setQuantity}
                  style={styles.input}
                  value={quantity}
                />

                <View style={styles.buttonRow}>
                  <Pressable onPress={createOrder} style={styles.primaryButton}>
                    <Text style={styles.primaryButtonText}>
                      {submitting ? 'Creating...' : 'Create order'}
                    </Text>
                  </Pressable>

                  <Pressable onPress={fetchOrders} style={styles.secondaryButton}>
                    <Text style={styles.secondaryButtonText}>Refresh list</Text>
                  </Pressable>
                </View>
              </View>

              <View style={styles.sectionHeader}>
                <Text style={styles.sectionTitle}>Orders</Text>
                {loadingOrders ? <ActivityIndicator color="#0f766e" /> : null}
              </View>
            </View>
          }
          ListEmptyComponent={
            <View style={styles.emptyState}>
              <Text style={styles.emptyTitle}>No orders yet</Text>
              <Text style={styles.emptyText}>
                Create your first order above, or tap Refresh list if the API already has data.
              </Text>
            </View>
          }
        />
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#f5efe2',
  },
  flex: {
    flex: 1,
  },
  content: {
    padding: 20,
    gap: 16,
  },
  headerSection: {
    gap: 16,
  },
  title: {
    fontSize: 32,
    fontWeight: '700',
    color: '#1d2a22',
  },
  subtitle: {
    fontSize: 16,
    lineHeight: 22,
    color: '#4f5d54',
  },
  panel: {
    backgroundColor: '#fffdf8',
    borderRadius: 20,
    padding: 16,
    gap: 10,
    borderWidth: 1,
    borderColor: '#d8cfba',
  },
  label: {
    fontSize: 14,
    fontWeight: '700',
    color: '#2a3b31',
  },
  input: {
    borderWidth: 1,
    borderColor: '#b9c1b2',
    borderRadius: 12,
    backgroundColor: '#ffffff',
    paddingHorizontal: 14,
    paddingVertical: 12,
    fontSize: 16,
    color: '#1d2a22',
  },
  helpText: {
    fontSize: 13,
    lineHeight: 18,
    color: '#5b695f',
  },
  buttonRow: {
    flexDirection: 'row',
    gap: 12,
    flexWrap: 'wrap',
    marginTop: 6,
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    paddingHorizontal: 18,
    paddingVertical: 14,
    borderRadius: 14,
  },
  primaryButtonText: {
    color: '#f4fffd',
    fontWeight: '700',
    fontSize: 16,
  },
  secondaryButton: {
    backgroundColor: '#e7efe9',
    paddingHorizontal: 18,
    paddingVertical: 14,
    borderRadius: 14,
  },
  secondaryButtonText: {
    color: '#244237',
    fontWeight: '700',
    fontSize: 16,
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
  },
  sectionTitle: {
    fontSize: 22,
    fontWeight: '700',
    color: '#1d2a22',
  },
  emptyState: {
    backgroundColor: '#fffdf8',
    borderRadius: 20,
    padding: 20,
    borderWidth: 1,
    borderColor: '#d8cfba',
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#213229',
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 15,
    lineHeight: 22,
    color: '#5b695f',
  },
  orderCard: {
    backgroundColor: '#fffdf8',
    borderRadius: 18,
    padding: 16,
    gap: 6,
    borderWidth: 1,
    borderColor: '#d8cfba',
    marginBottom: 12,
  },
  orderHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 12,
  },
  orderProduct: {
    flex: 1,
    fontSize: 18,
    fontWeight: '700',
    color: '#1f3128',
  },
  orderStatus: {
    color: '#0f766e',
    fontWeight: '700',
    maxWidth: 140,
    textAlign: 'right',
  },
  orderMeta: {
    fontSize: 13,
    color: '#5a655d',
  },
});
