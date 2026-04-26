import { useEffect, useState } from "react";
import { ApiError } from "../services/apiClient";
import type { SessionUser } from "../services/sessionStorage";
import {
  getBootstrap,
  getClientBankingOverview,
  getClientMerchantAcceptanceEligibility,
  getClientMerchantProfiles,
  getClientKycOverview,
  getClientProfile,
  getClientAccounts,
  getClientComplaints,
  getClientFixedDeposits,
  getClientLoanProducts,
  getClientLoans,
  getClientMerchants,
  getClientSessions,
  getClientStandingOrders,
  getClientStatements,
  type ClientAccount,
  type ClientBankingOverview,
  type ClientBootstrap,
  type ClientComplaint,
  type ClientFixedDeposit,
  type ClientKycOverview,
  type ClientLoanProduct,
  type ClientLoanSummary,
  type ClientMerchant,
  type ClientMerchantAcceptanceEligibility,
  type ClientMerchantProfile,
  type ClientProfile,
  type ClientSession,
  type ClientStandingOrder
} from "../services/clientChannelApi";
import type { ClientStatementSummary } from "../services/clientChannelApi";

interface ClientDataState {
  bootstrap: ClientBootstrap | null;
  profile: ClientProfile | null;
  kycOverview: ClientKycOverview | null;
  bankingOverview: ClientBankingOverview | null;
  accounts: ClientAccount[];
  merchants: ClientMerchant[];
  merchantAcceptanceEligibility: ClientMerchantAcceptanceEligibility | null;
  merchantProfiles: ClientMerchantProfile[];
  standingOrders: ClientStandingOrder[];
  fixedDeposits: ClientFixedDeposit[];
  loans: ClientLoanSummary[];
  loanProducts: ClientLoanProduct[];
  sessions: ClientSession[];
  complaints: ClientComplaint[];
  statements: ClientStatementSummary[];
  isLoading: boolean;
  permissionWarnings: string[];
  errorMessage: string | null;
}

const initialState: ClientDataState = {
  bootstrap: null,
  profile: null,
  kycOverview: null,
  bankingOverview: null,
  accounts: [],
  merchants: [],
  merchantAcceptanceEligibility: null,
  merchantProfiles: [],
  standingOrders: [],
  fixedDeposits: [],
  loans: [],
  loanProducts: [],
  sessions: [],
  complaints: [],
  statements: [],
  isLoading: true,
  permissionWarnings: [],
  errorMessage: null
};

export function useClientData(user: SessionUser | null) {
  const [state, setState] = useState<ClientDataState>(initialState);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      if (!user) {
        setState(initialState);
        return;
      }

      setState((current) => ({ ...current, isLoading: true, errorMessage: null, permissionWarnings: [] }));

      const warnings: string[] = [];

      const bootstrapPromise = getBootstrap().catch((error) => {
        if (error instanceof ApiError && error.status === 404) {
          warnings.push("No linked customer record was found for the signed-in identity.");
          return null;
        }

        throw error;
      });

      const profilePromise = getClientProfile().catch((error) => {
        if (error instanceof ApiError && error.status === 404) {
          warnings.push("Customer profile is not available until the signed-in identity is linked to a customer record.");
          return null;
        }

        throw error;
      });

      const kycOverviewPromise = getClientKycOverview().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          warnings.push("KYC refresh is not available until the signed-in identity is linked to a customer record.");
          return null;
        }

        throw error;
      });

      const accountsPromise = getClientAccounts().catch((error) => {
        if (error instanceof ApiError && error.status === 403) {
          warnings.push("The signed-in account can authenticate, but it does not have access to account data yet.");
          return [];
        }

        throw error;
      });

      const bankingOverviewPromise = getClientBankingOverview().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          warnings.push("Banking overview is not available until the signed-in identity is linked to a customer record.");
          return null;
        }

        throw error;
      });

      const merchantsPromise = getClientMerchants().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return [];
        }

        throw error;
      });

      const standingOrdersPromise = getClientStandingOrders().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return [];
        }

        throw error;
      });

      const merchantAcceptanceEligibilityPromise = getClientMerchantAcceptanceEligibility().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return null;
        }

        throw error;
      });

      const merchantProfilesPromise = getClientMerchantProfiles().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return [];
        }

        throw error;
      });

      const fixedDepositsPromise = getClientFixedDeposits().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return [];
        }

        throw error;
      });

      const loanProductsPromise = getClientLoanProducts().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return [];
        }

        throw error;
      });

      const loansPromise = getClientLoans().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          return [];
        }

        throw error;
      });

      const sessionsPromise = getClientSessions().catch((error) => {
        if (error instanceof ApiError && error.status === 403) {
          warnings.push("Session visibility is restricted for this account.");
          return [];
        }

        throw error;
      });

      const complaintsPromise = getClientComplaints().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          warnings.push("Complaint access is not available until the signed-in identity is linked to a customer record.");
          return [];
        }

        throw error;
      });

      const statementsPromise = getClientStatements().catch((error) => {
        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          warnings.push("Statements are not available until the signed-in identity is linked to a customer record.");
          return [];
        }

        throw error;
      });

      try {
        const [bootstrap, profile, kycOverview, bankingOverview, accounts, merchants, merchantAcceptanceEligibility, merchantProfiles, standingOrders, fixedDeposits, loanProducts, loans, sessions, complaints, statements] = await Promise.all([
          bootstrapPromise,
          profilePromise,
          kycOverviewPromise,
          bankingOverviewPromise,
          accountsPromise,
          merchantsPromise,
          merchantAcceptanceEligibilityPromise,
          merchantProfilesPromise,
          standingOrdersPromise,
          fixedDepositsPromise,
          loanProductsPromise,
          loansPromise,
          sessionsPromise,
          complaintsPromise,
          statementsPromise
        ]);
        if (!cancelled) {
          setState({
            bootstrap,
            profile,
            kycOverview,
            bankingOverview,
            accounts,
            merchants,
            merchantAcceptanceEligibility,
            merchantProfiles,
            standingOrders,
            fixedDeposits,
            loans,
            loanProducts,
            sessions,
            complaints,
            statements,
            isLoading: false,
            permissionWarnings: [...warnings, ...(bootstrap?.warnings ?? [])],
            errorMessage: null
          });
        }
      } catch (error) {
        if (!cancelled) {
          setState({
            bootstrap: null,
            profile: null,
            kycOverview: null,
            bankingOverview: null,
            accounts: [],
            merchants: [],
            merchantAcceptanceEligibility: null,
            merchantProfiles: [],
            standingOrders: [],
            fixedDeposits: [],
            loans: [],
            loanProducts: [],
            sessions: [],
            complaints: [],
            statements: [],
            isLoading: false,
            permissionWarnings: warnings,
            errorMessage: error instanceof Error ? error.message : "Unable to load client data."
          });
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, [user, reloadKey]);

  return {
    ...state,
    reload: () => setReloadKey((current) => current + 1)
  };
}
