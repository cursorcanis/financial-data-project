import * as signalR from '@microsoft/signalr';
import type { IngestionUpdate, HealthUpdate } from '../types';

class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000;

  // Event handlers
  private onIngestionUpdate: ((data: IngestionUpdate) => void) | null = null;
  private onHealthUpdate: ((data: HealthUpdate) => void) | null = null;
  private onDataReceived: ((data: any) => void) | null = null;
  private onConnectionStateChanged: ((connected: boolean) => void) | null = null;

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection() {
    const token = localStorage.getItem('authToken');
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/data', {
        accessTokenFactory: () => token || '',
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null; // Stop reconnecting
          }
          return Math.min(this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount), 30000);
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    if (!this.hubConnection) return;

    // Connection lifecycle events
    this.hubConnection.onreconnecting(() => {
      console.log('SignalR: Reconnecting...');
      this.isConnected = false;
      this.onConnectionStateChanged?.(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR: Reconnected');
      this.isConnected = true;
      this.reconnectAttempts = 0;
      this.onConnectionStateChanged?.(true);
    });

    this.hubConnection.onclose((error) => {
      console.error('SignalR: Connection closed', error);
      this.isConnected = false;
      this.onConnectionStateChanged?.(false);
      this.attemptReconnect();
    });

    // Data event handlers
    this.hubConnection.on('IngestionUpdate', (data: IngestionUpdate) => {
      console.log('Ingestion update received:', data);
      this.onIngestionUpdate?.(data);
    });

    this.hubConnection.on('HealthUpdate', (data: HealthUpdate) => {
      console.log('Health update received:', data);
      this.onHealthUpdate?.(data);
    });

    this.hubConnection.on('DataReceived', (data: any) => {
      console.log('Data received:', data);
      this.onDataReceived?.(data);
    });

    this.hubConnection.on('NotificationReceived', (notification: any) => {
      console.log('Notification received:', notification);
      // Handle notifications (could show toast, update UI, etc.)
    });
  }

  private async attemptReconnect() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error('SignalR: Max reconnection attempts reached');
      return;
    }

    this.reconnectAttempts++;
    const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
    
    console.log(`SignalR: Attempting reconnect ${this.reconnectAttempts}/${this.maxReconnectAttempts} in ${delay}ms`);
    
    setTimeout(async () => {
      try {
        await this.connect();
      } catch (error) {
        console.error('SignalR: Reconnection failed', error);
        this.attemptReconnect();
      }
    }, delay);
  }

  // Public methods
  async connect(): Promise<void> {
    if (!this.hubConnection) {
      this.initializeConnection();
    }

    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR: Already connected');
      return;
    }

    try {
      await this.hubConnection!.start();
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log('SignalR: Connected successfully');
      this.onConnectionStateChanged?.(true);

      // Subscribe to groups or perform initial setup
      await this.subscribeToUpdates();
    } catch (error) {
      console.error('SignalR: Failed to connect', error);
      this.isConnected = false;
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.stop();
        this.isConnected = false;
        console.log('SignalR: Disconnected');
        this.onConnectionStateChanged?.(false);
      } catch (error) {
        console.error('SignalR: Error during disconnect', error);
      }
    }
  }

  private async subscribeToUpdates(): Promise<void> {
    if (!this.isConnected || !this.hubConnection) return;

    try {
      // Subscribe to specific data streams
      await this.hubConnection.invoke('SubscribeToIngestionUpdates');
      await this.hubConnection.invoke('SubscribeToHealthUpdates');
      console.log('SignalR: Subscribed to updates');
    } catch (error) {
      console.error('SignalR: Failed to subscribe to updates', error);
    }
  }

  // Subscribe to specific data sources
  async subscribeToDataSource(dataSourceId: number): Promise<void> {
    if (!this.isConnected || !this.hubConnection) {
      throw new Error('SignalR: Not connected');
    }

    try {
      await this.hubConnection.invoke('SubscribeToDataSource', dataSourceId);
      console.log(`SignalR: Subscribed to data source ${dataSourceId}`);
    } catch (error) {
      console.error(`SignalR: Failed to subscribe to data source ${dataSourceId}`, error);
      throw error;
    }
  }

  async unsubscribeFromDataSource(dataSourceId: number): Promise<void> {
    if (!this.isConnected || !this.hubConnection) {
      throw new Error('SignalR: Not connected');
    }

    try {
      await this.hubConnection.invoke('UnsubscribeFromDataSource', dataSourceId);
      console.log(`SignalR: Unsubscribed from data source ${dataSourceId}`);
    } catch (error) {
      console.error(`SignalR: Failed to unsubscribe from data source ${dataSourceId}`, error);
      throw error;
    }
  }

  // Send messages to server
  async sendMessage(method: string, ...args: any[]): Promise<any> {
    if (!this.isConnected || !this.hubConnection) {
      throw new Error('SignalR: Not connected');
    }

    try {
      const result = await this.hubConnection.invoke(method, ...args);
      return result;
    } catch (error) {
      console.error(`SignalR: Failed to invoke ${method}`, error);
      throw error;
    }
  }

  // Event handler setters
  setIngestionUpdateHandler(handler: (data: IngestionUpdate) => void): void {
    this.onIngestionUpdate = handler;
  }

  setHealthUpdateHandler(handler: (data: HealthUpdate) => void): void {
    this.onHealthUpdate = handler;
  }

  setDataReceivedHandler(handler: (data: any) => void): void {
    this.onDataReceived = handler;
  }

  setConnectionStateHandler(handler: (connected: boolean) => void): void {
    this.onConnectionStateChanged = handler;
  }

  // Remove event handlers
  removeIngestionUpdateHandler(): void {
    this.onIngestionUpdate = null;
  }

  removeHealthUpdateHandler(): void {
    this.onHealthUpdate = null;
  }

  removeDataReceivedHandler(): void {
    this.onDataReceived = null;
  }

  removeConnectionStateHandler(): void {
    this.onConnectionStateChanged = null;
  }

  // Getters
  getConnectionState(): boolean {
    return this.isConnected;
  }

  getHubConnection(): signalR.HubConnection | null {
    return this.hubConnection;
  }
}

// Create singleton instance
const signalRService = new SignalRService();

// Export singleton
export default signalRService;

// Export types for convenience
export type { IngestionUpdate, HealthUpdate };
