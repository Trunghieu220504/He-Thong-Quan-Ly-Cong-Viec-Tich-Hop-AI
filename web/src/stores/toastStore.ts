import { create } from 'zustand'

export type ToastType = 'success' | 'info' | 'warning' | 'error'

export interface ToastMessage {
  id: string
  title: string
  message?: string
  type: ToastType
  duration?: number
}

interface ToastState {
  toasts: ToastMessage[]
  addToast: (toast: Omit<ToastMessage, 'id'>) => string
  removeToast: (id: string) => void
  clearAll: () => void
}

export const useToastStore = create<ToastState>((set) => ({
  toasts: [],

  addToast: (toast) => {
    const id = 'toast-' + Date.now() + '-' + Math.random().toString(36).slice(2, 7)
    const newToast: ToastMessage = { ...toast, id }

    set((state) => ({
      toasts: [...state.toasts, newToast],
    }))

    const duration = toast.duration ?? 4500
    if (duration > 0) {
      setTimeout(() => {
        set((state) => ({
          toasts: state.toasts.filter((t) => t.id !== id),
        }))
      }, duration)
    }

    return id
  },

  removeToast: (id) =>
    set((state) => ({
      toasts: state.toasts.filter((t) => t.id !== id),
    })),

  clearAll: () => set({ toasts: [] }),
}))

// Helper tiện ích để gọi toast từ bất cứ đâu mà không cần hook
export const toast = {
  success: (title: string, message?: string, duration?: number) =>
    useToastStore.getState().addToast({ title, message, type: 'success', duration }),
  info: (title: string, message?: string, duration?: number) =>
    useToastStore.getState().addToast({ title, message, type: 'info', duration }),
  warning: (title: string, message?: string, duration?: number) =>
    useToastStore.getState().addToast({ title, message, type: 'warning', duration }),
  error: (title: string, message?: string, duration?: number) =>
    useToastStore.getState().addToast({ title, message, type: 'error', duration }),
}

