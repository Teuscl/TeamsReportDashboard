// src/services/eventEmitter.ts
type EventCallback = (...args: any[]) => void;

interface Events {
  [eventName: string]: EventCallback[];
}

const events: Events = {};

export const eventEmitter = {
  subscribe: (eventName: string, callback: EventCallback): (() => void) => {
    if (!events[eventName]) {
      events[eventName] = [];
    }
    events[eventName].push(callback);
    // Retorna uma função para cancelar a inscrição (unsubscribe)
    return () => {
      if (events[eventName]) {
        events[eventName] = events[eventName].filter(cb => cb !== callback);
      }
    };
  },

  dispatch: (eventName: string, data?: any): void => {
    if (!events[eventName]) {
      return;
    }
    events[eventName].forEach(callback => {
      try {
        callback(data);
      } catch (error) {
        console.error(`Error in event listener for ${eventName}:`, error);
      }
    });
  },
};

// Define os nomes dos eventos para evitar typos
export const AUTH_EVENTS = {
  FORCE_LOGOUT: 'forceLogout',
};