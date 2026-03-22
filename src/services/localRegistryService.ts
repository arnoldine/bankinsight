import { MenuItem, UILayout } from '../../types';

const FORMS_KEY = 'bankinsight_custom_forms_v1';
const MENUS_KEY = 'bankinsight_custom_menus_v1';
const SUBMISSIONS_KEY = 'bankinsight_form_submissions_v1';

export type RegistryUser = {
  id: string;
  name: string;
  role?: string;
};

export type LocalFormSubmission = {
  id: string;
  formId: string;
  formName: string;
  submittedAt: string;
  submittedBy: string;
  status: 'LOCAL_ONLY' | 'STARTED' | 'FAILED';
  linkedWorkflow?: string;
  linkedTable?: string;
  correlationId?: string;
  instanceId?: string;
  message?: string;
  payload: Record<string, string | number>;
};

const defaultTemplateForms: UILayout[] = [
  {
    id: 'FORM-MOMO-CASHIER-TEMPLATE',
    name: 'Cashier Mobile Money Transaction',
    description:
      'Sample cashier-operated mobile money capture form for cash-in, cash-out, wallet transfer, and reversal workflows.',
    published: true,
    linkedTable: 'm_mobile_money_transaction',
    linkedWorkflow: 'MOMO_CASHIER_AUTOMATION',
    menuLabel: 'Mobile Money Cashier',
    menuIcon: 'Smartphone',
    createdBy: 'SYSTEM',
    createdByName: 'BankInsight',
    isTemplate: true,
    isLocked: true,
    createdAt: '2026-03-12T00:00:00.000Z',
    updatedAt: '2026-03-12T00:00:00.000Z',
    components: [
      {
        id: 'cmp-header-momo',
        type: 'HEADER',
        label: 'Cashier Mobile Money Transaction',
        width: 'FULL',
      },
      {
        id: 'cmp-transaction-type',
        type: 'SELECT',
        label: 'Transaction Type',
        width: 'HALF',
        required: true,
        placeholder: 'Cash in, cash out, wallet transfer, reversal',
        variableName: 'transaction_type',
      },
      {
        id: 'cmp-network',
        type: 'SELECT',
        label: 'Mobile Money Network',
        width: 'HALF',
        required: true,
        placeholder: 'MTN, Telecel, AirtelTigo',
        variableName: 'network_provider',
      },
      {
        id: 'cmp-wallet-number',
        type: 'TEXT_INPUT',
        label: 'Customer Wallet Number',
        width: 'HALF',
        required: true,
        placeholder: '0240000000',
        variableName: 'wallet_number',
      },
      {
        id: 'cmp-agent-till',
        type: 'TEXT_INPUT',
        label: 'Agent Till / Merchant ID',
        width: 'HALF',
        required: true,
        placeholder: 'AGENT-10023',
        variableName: 'agent_till_id',
      },
      {
        id: 'cmp-amount',
        type: 'NUMBER_INPUT',
        label: 'Cash Amount',
        width: 'HALF',
        required: true,
        placeholder: '0.00',
        variableName: 'transaction_amount',
      },
      {
        id: 'cmp-reference',
        type: 'TEXT_INPUT',
        label: 'External Reference',
        width: 'HALF',
        placeholder: 'MOMO-REF-20260312',
        variableName: 'external_reference',
      },
      {
        id: 'cmp-notes',
        type: 'TEXT_INPUT',
        label: 'Cashier Notes',
        width: 'FULL',
        placeholder: 'Exception notes, wallet owner confirmation, or follow-up instructions',
        variableName: 'cashier_notes',
      },
    ],
  },
];

const defaultMenuItems: MenuItem[] = [];

function safeParse<T>(value: string | null, fallback: T): T {
  if (!value) return fallback;
  try {
    return JSON.parse(value) as T;
  } catch {
    return fallback;
  }
}

function canUseStorage(): boolean {
  return typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
}

function cloneTemplateForms(): UILayout[] {
  return defaultTemplateForms.map((form) => ({ ...form, components: [...(form.components || [])] }));
}

export const localRegistryService = {
  getForms(): UILayout[] {
    if (!canUseStorage()) {
      return cloneTemplateForms();
    }

    const stored = safeParse<UILayout[]>(window.localStorage.getItem(FORMS_KEY), []);
    const templateIds = new Set(defaultTemplateForms.map((form) => form.id));
    const customForms = stored.filter((form) => !templateIds.has(form.id));
    return [...cloneTemplateForms(), ...customForms];
  },

  saveForm(layout: UILayout, user: RegistryUser): UILayout[] {
    if (!canUseStorage()) {
      return this.getForms();
    }

    const forms = this.getForms();
    const now = new Date().toISOString();
    const nextForm: UILayout = {
      ...layout,
      createdBy: layout.createdBy || user.id,
      createdByName: layout.createdByName || user.name,
      isTemplate: layout.isTemplate || false,
      isLocked: layout.isLocked || false,
      createdAt: layout.createdAt || now,
      updatedAt: now,
    };

    const templateIds = new Set(defaultTemplateForms.map((form) => form.id));
    const nextForms = [
      ...cloneTemplateForms(),
      ...forms
        .filter((form) => !templateIds.has(form.id) && form.id !== nextForm.id)
        .concat(nextForm),
    ];

    window.localStorage.setItem(
      FORMS_KEY,
      JSON.stringify(nextForms.filter((form) => !templateIds.has(form.id)))
    );
    return nextForms;
  },

  getMenuItems(): MenuItem[] {
    if (!canUseStorage()) {
      return [...defaultMenuItems];
    }

    const stored = safeParse<MenuItem[]>(window.localStorage.getItem(MENUS_KEY), []);
    const defaultIds = new Set(defaultMenuItems.map((item) => item.id));
    const customMenus = stored.filter((item) => !defaultIds.has(item.id));
    return [...defaultMenuItems, ...customMenus];
  },

  saveMenuItem(item: MenuItem): MenuItem[] {
    if (!canUseStorage()) {
      return this.getMenuItems();
    }

    const menuItems = this.getMenuItems();
    const defaultIds = new Set(defaultMenuItems.map((menu) => menu.id));
    const next = [
      ...defaultMenuItems,
      ...menuItems
        .filter((menu) => !defaultIds.has(menu.id) && menu.id !== item.id)
        .concat(item),
    ];

    window.localStorage.setItem(
      MENUS_KEY,
      JSON.stringify(next.filter((menu) => !defaultIds.has(menu.id)))
    );
    return next;
  },

  deleteMenuItem(id: string): MenuItem[] {
    if (!canUseStorage()) {
      return this.getMenuItems();
    }

    const defaultIds = new Set(defaultMenuItems.map((menu) => menu.id));
    const next = this.getMenuItems().filter((item) => defaultIds.has(item.id) || item.id !== id);
    window.localStorage.setItem(
      MENUS_KEY,
      JSON.stringify(next.filter((menu) => !defaultIds.has(menu.id)))
    );
    return next;
  },

  getFormSubmissions(formId: string): LocalFormSubmission[] {
    if (!canUseStorage()) {
      return [];
    }

    const submissions = safeParse<LocalFormSubmission[]>(window.localStorage.getItem(SUBMISSIONS_KEY), []);
    return submissions.filter((submission) => submission.formId === formId);
  },

  saveFormSubmission(submission: LocalFormSubmission): LocalFormSubmission[] {
    if (!canUseStorage()) {
      return [submission];
    }

    const submissions = safeParse<LocalFormSubmission[]>(window.localStorage.getItem(SUBMISSIONS_KEY), []);
    const next = [submission, ...submissions.filter((item) => item.id !== submission.id)].slice(0, 100);
    window.localStorage.setItem(SUBMISSIONS_KEY, JSON.stringify(next));
    return next;
  },
};
