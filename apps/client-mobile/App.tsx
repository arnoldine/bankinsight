import { StatusBar } from "expo-status-bar";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { AppShell } from "./src/AppShell";
import { DevModeBanner } from "./src/components/DevModeBanner";
import { DevicePreviewFrame } from "./src/components/DevicePreviewFrame";
import { SessionProvider, useSession } from "./src/context/SessionContext";
import { SignInScreen } from "./src/screens/SignInScreen";

function AppGate() {
  const { isHydrating, isAuthenticated } = useSession();

  if (isHydrating) {
    return <SignInScreen mode="loading" />;
  }

  return isAuthenticated ? <AppShell /> : <SignInScreen mode="idle" />;
}

export default function App() {
  return (
    <SafeAreaProvider>
      <SessionProvider>
        <StatusBar style="dark" />
        <DevModeBanner />
        <DevicePreviewFrame>
          <AppGate />
        </DevicePreviewFrame>
      </SessionProvider>
    </SafeAreaProvider>
  );
}
