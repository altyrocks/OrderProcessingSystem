import { ScrollView, StyleSheet, Text, View } from 'react-native';

export default function LearnScreen() {
  return (
    <ScrollView contentContainerStyle={styles.content}>
      <Text style={styles.title}>How This App Works</Text>
      <Text style={styles.intro}>
        This tab is here to explain the moving pieces in plain English while you learn React Native
        and the backend architecture at the same time.
      </Text>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>1. The home screen is stateful</Text>
        <Text style={styles.body}>
          In React Native, state means values the screen remembers while it is open. We use state
          for the API URL, the form fields, the current list of orders, and loading flags.
        </Text>
      </View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>2. fetch talks to your API</Text>
        <Text style={styles.body}>
          When you tap Create order, the app sends a POST request to Orders.Api. When you tap
          Refresh list, it sends a GET request to load all orders again.
        </Text>
      </View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>3. FlatList renders repeating data</Text>
        <Text style={styles.body}>
          A FlatList is React Native&apos;s efficient way to display many rows. Each order becomes one
          card showing product name, quantity, id, and current status.
        </Text>
      </View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>4. Polling keeps statuses fresh</Text>
        <Text style={styles.body}>
          The backend saga changes status over time, so the app polls every two seconds after a new
          order is created. That lets you watch Pending turn into Inventory Reserved and then a final
          result.
        </Text>
      </View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>5. localhost can be tricky on mobile</Text>
        <Text style={styles.body}>
          If you run the app in a simulator, localhost may refer to the simulator itself rather than
          your computer. That is why the app includes an editable API base URL field.
        </Text>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    padding: 20,
    backgroundColor: '#f3efe6',
    gap: 16,
  },
  title: {
    fontSize: 30,
    fontWeight: '700',
    color: '#1e2d25',
  },
  intro: {
    fontSize: 16,
    lineHeight: 24,
    color: '#546259',
  },
  card: {
    backgroundColor: '#fffdf8',
    borderRadius: 18,
    padding: 18,
    borderWidth: 1,
    borderColor: '#d9d1bf',
    gap: 8,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#24352c',
  },
  body: {
    fontSize: 15,
    lineHeight: 22,
    color: '#5b685f',
  },
});
