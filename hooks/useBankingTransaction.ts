import { useState } from 'react';
import { Transaction } from '../types';

interface TransactionResult {
  success: boolean;
  message: string;
  receiptId?: string;
  newBalance?: number;
}

export const useBankingTransaction = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const postTransaction = async (tx: Omit<Transaction, 'id' | 'status' | 'date'>): Promise<TransactionResult> => {
    setLoading(true);
    setError(null);

    return new Promise((resolve) => {
      setTimeout(() => {
        // Simulate Network/API Check
        const isOffline = Math.random() > 0.95; // 5% chance of network failure simulation
        
        if (isOffline) {
            setLoading(false);
            const msg = "ERR_NET_01: O4W API Gateway Unreachable. Transaction queued locally.";
            setError(msg);
            resolve({ success: false, message: msg });
            return;
        }

        // Simulate PL/pgSQL Validation Logic
        if (tx.type === 'WITHDRAWAL' && tx.amount > 10000) {
           // AML Limit Check
           setLoading(false);
           resolve({ 
               success: false, 
               message: "AML_09: Amount exceeds Tier 2 daily withdrawal limit. Manager override required." 
           });
           return;
        }

        setLoading(false);
        resolve({
          success: true,
          message: "Transaction Posted Successfully to GL.",
          receiptId: `RCPT-${Date.now().toString().slice(-6)}`,
          newBalance: 0 // Mock, would be real in full app
        });
      }, 1500);
    });
  };

  return { postTransaction, loading, error };
};