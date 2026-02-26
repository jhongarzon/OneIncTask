import * as signalR from '@microsoft/signalr';

let connection: signalR.HubConnection | null = null;
let currentToken: string | null = null;

export function getConnection(token: string): signalR.HubConnection {
  if (connection && currentToken === token) {
    return connection;
  }

  if (connection) {
    connection.stop();
    connection = null;
  }

  currentToken = token;
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hub/job-progress', {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}

export async function stopConnection(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}

export function getExistingConnection(): signalR.HubConnection | null {
  return connection;
}
