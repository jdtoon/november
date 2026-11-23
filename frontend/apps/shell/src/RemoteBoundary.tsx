import { Component } from "react";
import type { ErrorInfo, ReactNode } from "react";

type Props = {
  fallback: (error: Error) => ReactNode;
  children: ReactNode;
};

type State = { error: Error | null };

export class RemoteBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("A remote app failed", error, info.componentStack);
  }

  render() {
    return this.state.error ? this.props.fallback(this.state.error) : this.props.children;
  }
}
