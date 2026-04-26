import { Platform } from "react-native";

export const colors = {
  backgroundTop: "#edf3f9",
  backgroundBottom: "#dbe6f1",
  backgroundVeil: "rgba(248, 251, 255, 0.82)",
  surface: "#fbfdff",
  surfaceSoft: "#f1f5f9",
  surfaceMuted: "#e6edf5",
  surfaceStrong: "#10233a",
  border: "#d3dce7",
  borderStrong: "#afbccb",
  text: "#0f1824",
  textSoft: "#243447",
  muted: "#617285",
  forest: "#13314f",
  forestSoft: "#29557f",
  forestDeep: "#0b1828",
  copper: "#2e6fb3",
  copperSoft: "#9ec2e8",
  gold: "#4e86c5",
  goldSoft: "#d5e7fb",
  stable: "#166a53",
  warning: "#9e670f",
  critical: "#b23833",
  white: "#f9fbfe",
  inkInverse: "#f5f8fc"
} as const;

export const spacing = {
  xs: 8,
  sm: 12,
  md: 16,
  lg: 20,
  xl: 24,
  xxl: 32
} as const;

export const typography = {
  display: Platform.select({
    ios: "Avenir Next",
    android: "sans-serif-medium",
    default: 'Inter, "Segoe UI", system-ui, sans-serif'
  }),
  body: Platform.select({
    ios: "System",
    android: "sans-serif",
    default: '"Segoe UI", system-ui, sans-serif'
  }),
  mono: Platform.select({
    ios: "Menlo",
    android: "monospace",
    default: '"SFMono-Regular", Consolas, monospace'
  })
} as const;
