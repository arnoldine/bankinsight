import { useEffect, useMemo, useState } from 'react';
import { platformEnhancementService, type DeveloperPortalSummary } from '../services/platformEnhancementService';

const dateText = (value?: string | null) => value ? new Date(value).toLocaleString() : 'Not set';

export default function DeveloperPortalHub() {
  const [summary, setSummary] = useState<DeveloperPortalSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: '',
    partnerName: '',
    callbackUrl: '',
    contactEmail: '',
    apiProductIds: [] as string[],
  });
  const [webhookForm, setWebhookForm] = useState({
    partnerApplicationId: '',
    eventName: '',
    targetUrl: '',
  });
  const [replayForm, setReplayForm] = useState({
    webhookSubscriptionId: '',
    eventName: '',
  });

  const loadSummary = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await platformEnhancementService.getDeveloperPortalSummary();
      setSummary(data);
      setWebhookForm((current) => ({
        ...current,
        partnerApplicationId: current.partnerApplicationId || data.partnerApplications[0]?.id || '',
        eventName: current.eventName || data.eventCatalog[0]?.eventName || '',
      }));
      setReplayForm((current) => ({
        webhookSubscriptionId: current.webhookSubscriptionId || data.webhookSubscriptions[0]?.id || '',
        eventName: current.eventName || data.webhookSubscriptions[0]?.eventName || data.eventCatalog[0]?.eventName || '',
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load developer portal.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadSummary();
  }, []);

  const products = summary?.products ?? [];
  const apps = summary?.partnerApplications ?? [];
  const subscriptions = summary?.webhookSubscriptions ?? [];
  const events = summary?.eventCatalog ?? [];
  const metrics = summary?.metrics ?? [];
  const deliveryLogs = summary?.deliveryLogs ?? [];

  const selectedProductsLabel = useMemo(
    () => products.filter((product) => form.apiProductIds.includes(product.id)).map((product) => product.name).join(', '),
    [form.apiProductIds, products],
  );

  const toggleProduct = (productId: string) => {
    setForm((current) => ({
      ...current,
      apiProductIds: current.apiProductIds.includes(productId)
        ? current.apiProductIds.filter((id) => id !== productId)
        : [...current.apiProductIds, productId],
    }));
  };

  const createPartnerApp = async () => {
    await platformEnhancementService.createPartnerApplication(form);
    setForm({ name: '', partnerName: '', callbackUrl: '', contactEmail: '', apiProductIds: [] });
    await loadSummary();
  };

  const createWebhook = async () => {
    await platformEnhancementService.createWebhookSubscription(webhookForm);
    setWebhookForm((current) => ({ ...current, targetUrl: '' }));
    await loadSummary();
  };

  const rotateKey = async (id: string) => {
    await platformEnhancementService.rotatePartnerSandboxKey(id);
    await loadSummary();
  };

  const promoteApp = async (id: string) => {
    await platformEnhancementService.promotePartnerApplication(id, { environment: 'PRODUCTION' });
    await loadSummary();
  };

  const replayWebhook = async () => {
    if (!replayForm.webhookSubscriptionId || !replayForm.eventName) {
      return;
    }
    await platformEnhancementService.replayWebhook(replayForm);
    await loadSummary();
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Developer Portal</p>
        <h1 className="mt-2 text-2xl font-semibold text-slate-950">API productization and partner onboarding</h1>
        <p className="mt-2 text-sm text-slate-600">Manage published APIs, partner applications, credential lifecycle, and webhook delivery governance from one workspace.</p>
      </div>

      {error && <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>}

      {isLoading ? (
        <div className="rounded-3xl border border-slate-200 bg-white p-8 text-sm text-slate-500">Loading developer portal...</div>
      ) : (
        <>
          <div className="grid gap-4 md:grid-cols-4">
            {metrics.map((metric) => (
              <div key={metric.key} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <p className="text-xs uppercase tracking-[0.2em] text-slate-400">{metric.label}</p>
                <p className="mt-2 text-2xl font-semibold text-slate-950">{metric.value}</p>
              </div>
            ))}
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Published API Products</h2>
              <div className="mt-4 grid gap-4 md:grid-cols-2">
                {products.map((product) => (
                  <div key={product.id} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{product.name}</p>
                        <p className="text-xs text-slate-500">{product.basePath}</p>
                      </div>
                      <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{product.version}</span>
                    </div>
                    <p className="mt-3 text-sm text-slate-600">{product.scopeSummary}</p>
                    <div className="mt-3 flex flex-wrap gap-2 text-xs text-slate-500">
                      <span className="rounded-full bg-slate-50 px-3 py-1">{product.category}</span>
                      <span className="rounded-full bg-slate-50 px-3 py-1">{product.authModel}</span>
                      <span className="rounded-full bg-slate-50 px-3 py-1">{product.rateLimitPerMinute}/min</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Register Partner App</h2>
                <div className="mt-4 space-y-3">
                  <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Application name" />
                  <input value={form.partnerName} onChange={(e) => setForm({ ...form, partnerName: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Partner organization" />
                  <input value={form.contactEmail} onChange={(e) => setForm({ ...form, contactEmail: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Contact email" />
                  <input value={form.callbackUrl} onChange={(e) => setForm({ ...form, callbackUrl: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Callback URL" />
                  <div className="rounded-2xl border border-slate-200 p-4">
                    <p className="text-sm font-semibold text-slate-800">API products</p>
                    <div className="mt-3 space-y-2">
                      {products.map((product) => (
                        <label key={product.id} className="flex items-center gap-3 text-sm text-slate-700">
                          <input type="checkbox" checked={form.apiProductIds.includes(product.id)} onChange={() => toggleProduct(product.id)} />
                          <span>{product.name}</span>
                        </label>
                      ))}
                    </div>
                    {selectedProductsLabel && <p className="mt-3 text-xs text-slate-500">Selected: {selectedProductsLabel}</p>}
                  </div>
                  <button type="button" onClick={() => void createPartnerApp()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white">
                    Create partner app
                  </button>
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Webhook controls</h2>
                <div className="mt-4 space-y-3">
                  <select value={webhookForm.partnerApplicationId} onChange={(e) => setWebhookForm({ ...webhookForm, partnerApplicationId: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm">
                    {apps.map((app) => <option key={app.id} value={app.id}>{app.partnerName} / {app.name}</option>)}
                  </select>
                  <select value={webhookForm.eventName} onChange={(e) => setWebhookForm({ ...webhookForm, eventName: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm">
                    {events.map((item) => <option key={item.eventName} value={item.eventName}>{item.eventName}</option>)}
                  </select>
                  <input value={webhookForm.targetUrl} onChange={(e) => setWebhookForm({ ...webhookForm, targetUrl: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm" placeholder="Target URL" />
                  <button type="button" onClick={() => void createWebhook()} className="rounded-full bg-slate-950 px-4 py-2 text-sm font-semibold text-white">
                    Create subscription
                  </button>
                  <div className="border-t border-slate-100 pt-3">
                    <p className="text-sm font-semibold text-slate-900">Replay failed delivery</p>
                    <div className="mt-3 space-y-3">
                      <select value={replayForm.webhookSubscriptionId} onChange={(e) => setReplayForm({ ...replayForm, webhookSubscriptionId: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm">
                        {subscriptions.map((item) => <option key={item.id} value={item.id}>{item.partnerApplicationName} / {item.eventName}</option>)}
                      </select>
                      <select value={replayForm.eventName} onChange={(e) => setReplayForm({ ...replayForm, eventName: e.target.value })} className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm">
                        {events.map((item) => <option key={item.eventName} value={item.eventName}>{item.eventName}</option>)}
                      </select>
                      <button type="button" onClick={() => void replayWebhook()} className="rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700">
                        Replay webhook
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="grid gap-6 xl:grid-cols-2">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-slate-950">Partner Applications</h2>
              <div className="mt-4 space-y-3">
                {apps.map((app) => (
                  <div key={app.id} className="rounded-2xl border border-slate-200 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{app.partnerName}</p>
                        <p className="text-xs text-slate-500">{app.name}</p>
                      </div>
                      <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{app.environment}</span>
                    </div>
                    <div className="mt-3 space-y-1 text-xs text-slate-500">
                      <p>Status: {app.status}</p>
                      <p>Sandbox key: {app.sandboxKeyPreview}</p>
                      <p>Production key: {app.productionKeyPreview || 'Not activated'}</p>
                      <p>Production activation: {dateText(app.productionKeyActivatedAt)}</p>
                      <p>Last activity: {dateText(app.lastActivityAt)}</p>
                      <p>Callback: {app.callbackUrl}</p>
                    </div>
                    <div className="mt-3 flex flex-wrap justify-end gap-2">
                      <button type="button" onClick={() => void rotateKey(app.id)} className="rounded-full border border-slate-300 px-4 py-2 text-xs font-semibold text-slate-700">
                        Rotate sandbox key
                      </button>
                      {app.environment !== 'PRODUCTION' && (
                        <button type="button" onClick={() => void promoteApp(app.id)} className="rounded-full bg-slate-950 px-4 py-2 text-xs font-semibold text-white">
                          Promote to production
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="space-y-6">
              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Webhook Subscriptions</h2>
                <div className="mt-4 space-y-3">
                  {subscriptions.map((subscription) => (
                    <div key={subscription.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{subscription.eventName}</p>
                          <p className="text-xs text-slate-500">{subscription.partnerApplicationName}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{subscription.status}</span>
                      </div>
                      <p className="mt-2 text-xs text-slate-500">{subscription.targetUrl}</p>
                      <p className="mt-1 text-xs text-slate-500">Secret: {subscription.signingSecretPreview}</p>
                      <p className="mt-1 text-xs text-slate-500">Last delivery: {dateText(subscription.lastDeliveryAt)} / {subscription.lastDeliveryStatus || 'No delivery yet'}</p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-slate-950">Delivery log</h2>
                <div className="mt-4 space-y-3">
                  {deliveryLogs.map((log) => (
                    <div key={log.id} className="rounded-2xl border border-slate-200 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-950">{log.eventName}</p>
                          <p className="text-xs text-slate-500">Attempt {log.attemptNumber} / Response {log.responseCode ?? 'N/A'}</p>
                        </div>
                        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{log.deliveryStatus}</span>
                      </div>
                      <p className="mt-2 text-xs text-slate-500">Delivered at: {dateText(log.deliveredAt)}</p>
                      {log.failureReason && <p className="mt-1 text-xs text-rose-600">Failure: {log.failureReason}</p>}
                    </div>
                  ))}
                  {deliveryLogs.length === 0 && (
                    <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-5 text-sm text-slate-500">
                      No delivery logs yet.
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
