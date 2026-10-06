import { Component, type ErrorInfo, type ReactNode } from 'react';
import { logger } from '../lib/logger';

interface ErrorBoundaryProps {
  readonly children: ReactNode;
}

interface ErrorBoundaryState {
  readonly hasError: boolean;
}

/** Last line of defence: an unexpected render error shows a message, not a blank page. */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  override state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  override componentDidCatch(error: Error, info: ErrorInfo): void {
    logger.error('ui.unhandledRenderError', {
      message: error.message,
      componentStack: info.componentStack ?? undefined,
    });
  }

  override render(): ReactNode {
    if (this.state.hasError) {
      return (
        <div className="alert" role="alert">
          <p>Sorry, something went wrong. Please refresh the page.</p>
        </div>
      );
    }
    return this.props.children;
  }
}
