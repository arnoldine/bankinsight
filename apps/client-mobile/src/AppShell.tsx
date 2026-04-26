import { Ionicons } from "@expo/vector-icons";
import { DefaultTheme, NavigationContainer } from "@react-navigation/native";
import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { Pressable } from "react-native";
import { AccountsScreen } from "./screens/AccountsScreen";
import { AlertsScreen } from "./screens/AlertsScreen";
import { BankingScreen } from "./screens/BankingScreen";
import { ComplaintsScreen } from "./screens/ComplaintsScreen";
import { HomeScreen } from "./screens/HomeScreen";
import { MoreScreen } from "./screens/MoreScreen";
import { ProfileScreen } from "./screens/ProfileScreen";
import { SecurityScreen } from "./screens/SecurityScreen";
import { StatementsScreen } from "./screens/StatementsScreen";
import { useSession } from "./context/SessionContext";
import { colors, typography } from "./theme";

const Tab = createBottomTabNavigator();
const Stack = createNativeStackNavigator();

const navigationTheme = {
  ...DefaultTheme,
  colors: {
    ...DefaultTheme.colors,
    background: colors.backgroundBottom,
    card: colors.surface,
    text: colors.text,
    border: colors.border,
    primary: colors.forest
  }
};

function iconName(routeName: string): keyof typeof Ionicons.glyphMap {
  switch (routeName) {
    case "Home":
      return "home-outline";
    case "Banking":
      return "swap-horizontal-outline";
    case "Accounts":
      return "wallet-outline";
    case "Support":
      return "chatbox-ellipses-outline";
    case "More":
      return "grid-outline";
    default:
      return "ellipse-outline";
  }
}

function MainTabs() {
  const { displayName, signOut } = useSession();

  return (
    <Tab.Navigator
      id="main-tabs"
      screenOptions={({ route }) => ({
        headerStyle: { backgroundColor: colors.surface },
        headerTintColor: colors.text,
        headerShadowVisible: false,
        headerTitleStyle: { fontWeight: "800", fontFamily: typography.display, fontSize: 18 },
        headerRight: () => (
          <Pressable
            onPress={() => {
              void signOut();
            }}
            hitSlop={8}
          >
            <Ionicons name="log-out-outline" size={22} color={colors.forest} />
          </Pressable>
        ),
        tabBarStyle: {
          backgroundColor: colors.surface,
          borderTopColor: colors.border,
          height: 70,
          paddingBottom: 8,
          paddingTop: 8
        },
        tabBarLabelStyle: {
          fontSize: 11,
          fontWeight: "700",
          fontFamily: typography.body,
          letterSpacing: 0.2
        },
        tabBarActiveTintColor: colors.forest,
        tabBarInactiveTintColor: colors.muted,
        tabBarIcon: ({ color, size }) => <Ionicons name={iconName(route.name)} size={size} color={color} />
      })}
    >
      <Tab.Screen name="Home" component={HomeScreen} options={{ headerTitle: `${displayName.split(" ")[0]}'s Home` }} />
      <Tab.Screen name="Banking" component={BankingScreen} />
      <Tab.Screen name="Accounts" component={AccountsScreen} />
      <Tab.Screen name="Support" component={ComplaintsScreen} options={{ headerTitle: "Complaints and support" }} />
      <Tab.Screen name="More" component={MoreScreen} />
    </Tab.Navigator>
  );
}

export function AppShell() {
  return (
    <NavigationContainer theme={navigationTheme}>
      <Stack.Navigator
        id="root-stack"
        screenOptions={{
          headerStyle: { backgroundColor: colors.surface },
          headerTintColor: colors.text,
          headerShadowVisible: false,
          headerTitleStyle: { fontWeight: "800", fontFamily: typography.display, fontSize: 18 }
        }}
      >
        <Stack.Screen name="MainTabs" component={MainTabs} options={{ headerShown: false }} />
        <Stack.Screen name="StatementsHub" component={StatementsScreen} options={{ title: "Statements" }} />
        <Stack.Screen name="AlertsHub" component={AlertsScreen} options={{ title: "Alerts" }} />
        <Stack.Screen name="ProfileHub" component={ProfileScreen} options={{ title: "Profile and KYC" }} />
        <Stack.Screen name="SecurityHub" component={SecurityScreen} options={{ title: "Security center" }} />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
