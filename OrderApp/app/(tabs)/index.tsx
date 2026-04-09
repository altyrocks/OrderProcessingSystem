import { useState } from "react";
import { Button, StyleSheet, Text, View } from "react-native";

export default function HomeScreen() {
  const [orderId, setOrderId] = useState<string | null>(null);
  const [status, setStatus] = useState("");

  const createOrder = async () => {
    const response = await fetch("https://localhost:7150/api/orders", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ item: "Laptop", quantity: 1 }),
    });

    const data = await response.json();
    setOrderId(data.orderId);
    setStatus("Pending");

    pollStatus(data.orderId);
  };

  const pollStatus = (id: string) => {
    const interval = setInterval(async () => {
      const res = await fetch(`https://localhost:7150/api/orders/${id}`);
      const data = await res.json();

      setStatus(data.status);

      if (data.status !== "Pending") {
        clearInterval(interval);
      }
    }, 2000);
  };

  return (
    <View style={styles.container}>
      <Button title="Create Order" onPress={createOrder} />

      {orderId && (
        <>
          <Text>Order ID: {orderId}</Text>
          <Text>Status: {status}</Text>
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
  },
});
