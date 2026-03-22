import React, { useMemo, useState } from 'react';
import { BarChart3, CalendarDays, FolderKanban, HandCoins, MapPinned, ShieldAlert, Users } from 'lucide-react';
import type { Customer, GroupLoanApplication, GroupMeeting, GroupPortfolioSummary, LendingCenter, LendingGroup, Product } from '../../../types';

interface GroupLendingHubProps {
  groups: LendingGroup[];
  centers: LendingCenter[];
  products: Product[];
  customers: Customer[];
  applications: GroupLoanApplication[];
  meetings: GroupMeeting[];
  portfolioSummary?: GroupPortfolioSummary | null;
  parReport: Array<any>;
  officerPerformance: Array<any>;
  cycleAnalysis: Array<any>;
  delinquencyReport: Array<any>;
  meetingCollectionsReport: Array<any>;
  onCreateGroup: (payload: Record<string, unknown>) => Promise<void>;
  onAddMember: (groupId: string, payload: Record<string, unknown>) => Promise<void>;
  onCreateCenter: (payload: Record<string, unknown>) => Promise<void>;
  onCreateApplication: (payload: Record<string, unknown>) => Promise<void>;
  onSubmitApplication: (id: string) => Promise<void>;
  onApproveApplication: (id: string) => Promise<void>;
  onRejectApplication: (id: string) => Promise<void>;
  onDisburseApplication: (id: string) => Promise<void>;
  onCreateMeeting: (payload: Record<string, unknown>) => Promise<void>;
}

const tabs = [
  { id: 'dashboard', label: 'Dashboard', icon: BarChart3 },
  { id: 'setup', label: 'Groups', icon: Users },
  { id: 'applications', label: 'Applications', icon: FolderKanban },
  { id: 'meetings', label: 'Meetings', icon: CalendarDays },
  { id: 'reports', label: 'Reports', icon: ShieldAlert },
] as const;

const GroupLendingHub: React.FC<GroupLendingHubProps> = (props) => {
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]['id']>('dashboard');
  const [draftGroup, setDraftGroup] = useState({ groupName: '', meetingDayOfWeek: 'Tuesday', branchId: 'BR001', meetingFrequency: 'Weekly' });
  const [draftCenter, setDraftCenter] = useState({ branchId: 'BR001', centerCode: '', centerName: '', meetingDayOfWeek: 'Tuesday' });
  const [selectedGroupId, setSelectedGroupId] = useState<string>('');
  const [selectedCustomerId, setSelectedCustomerId] = useState<string>('');
  const [draftMeeting, setDraftMeeting] = useState({ groupId: '', meetingDate: new Date().toISOString().slice(0, 10), meetingType: 'REGULAR' });
  const [applicationGroupId, setApplicationGroupId] = useState<string>('');
  const [applicationProductId, setApplicationProductId] = useState<string>('');
  const selectedGroup = useMemo(() => props.groups.find(g => g.id === selectedGroupId) || props.groups[0], [props.groups, selectedGroupId]);
  const groupLoanProducts = props.products.filter(p => p.type === 'LOAN' && (p.isGroupLoanEnabled || p.lendingMethodology === 'GROUP' || p.lendingMethodology === 'HYBRID'));

  const createCycleApplication = async () => {
    const group = props.groups.find(g => g.id === applicationGroupId);
    const product = props.products.find(p => p.id === applicationProductId);
    if (!group || !product) return;
    await props.onCreateApplication({
      groupId: group.id,
      productId: product.id,
      branchId: group.branchId,
      loanCycleNo: 0,
      members: group.members.filter(m => m.status === 'ACTIVE').map(member => ({
        groupMemberId: member.id,
        requestedAmount: Math.max(product.minAmount || 0, 500),
        tenureWeeks: product.groupRules?.maxWeeks || product.defaultTerm || 16,
        interestRate: product.interestRate || 0,
        interestMethod: product.groupRules?.defaultInterestMethod || product.interestMethod || 'Flat',
        repaymentFrequency: product.groupRules?.defaultRepaymentFrequency || product.defaultRepaymentFrequency || 'Weekly',
        loanPurpose: 'Working capital',
        savingsBalanceAtApplication: 0,
      })),
    });
  };

  return (
    <div className="simple-screen h-full rounded-lg border border-slate-200 bg-slate-50 p-6 overflow-y-auto">
      <div className="mb-6 flex flex-wrap gap-3">
        {tabs.map(tab => {
          const Icon = tab.icon;
          return (
            <button key={tab.id} onClick={() => setActiveTab(tab.id)} className={`flex items-center gap-2 px-4 py-2 text-sm font-semibold ${activeTab === tab.id ? 'screen-tab screen-tab-active' : 'screen-tab'}`}>
              <Icon size={16} /> {tab.label}
            </button>
          );
        })}
      </div>

      {activeTab === 'dashboard' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 xl:grid-cols-6 gap-4">
            {[
              ['Active Groups', props.portfolioSummary?.activeGroups || 0],
              ['Active Members', props.portfolioSummary?.activeMembers || 0],
              ['Portfolio', props.portfolioSummary?.totalPortfolio || 0],
              ['PAR 30', props.portfolioSummary?.par30 || 0],
              ['Due This Week', props.portfolioSummary?.weeklyDueThisWeek || 0],
              ['Collections', props.portfolioSummary?.collectionsThisWeek || 0],
            ].map(([label, value]) => (
              <div key={String(label)} className="screen-stat p-4">
                <div className="text-xs text-slate-500">{label}</div>
                <div className="mt-2 text-2xl font-bold text-slate-900">{typeof value === 'number' ? value.toLocaleString() : value}</div>
              </div>
            ))}
          </div>
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-5">
            <section className="rounded-2xl bg-white border border-slate-200 p-5">
              <h3 className="font-bold text-slate-900 mb-3">PAR Watchlist</h3>
              <div className="space-y-3 max-h-72 overflow-auto">
                {props.parReport.slice(0, 8).map((item, index) => <div key={index} className="flex justify-between text-sm border-b border-slate-100 pb-2"><span>{item.groupName}</span><span>{item.parBucket} / {item.daysPastDue} dpd</span></div>)}
              </div>
            </section>
            <section className="rounded-2xl bg-white border border-slate-200 p-5">
              <h3 className="font-bold text-slate-900 mb-3">Officer Performance</h3>
              <div className="space-y-3 max-h-72 overflow-auto">
                {props.officerPerformance.slice(0, 8).map((item, index) => <div key={index} className="flex justify-between text-sm border-b border-slate-100 pb-2"><span>{item.officerId}</span><span>{item.activeGroups} groups / {item.activeMembers} members</span></div>)}
              </div>
            </section>
          </div>
        </div>
      )}

      {activeTab === 'setup' && (
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
          <section className="rounded-2xl bg-white border border-slate-200 p-5 space-y-3">
            <h3 className="font-bold text-slate-900">Create Lending Group</h3>
            <input className="w-full p-2 border rounded-lg" placeholder="Group name" value={draftGroup.groupName} onChange={e => setDraftGroup({ ...draftGroup, groupName: e.target.value })} />
            <input className="w-full p-2 border rounded-lg" placeholder="Meeting location" onChange={e => setDraftGroup({ ...draftGroup, meetingLocation: e.target.value } as any)} />
            <button onClick={() => props.onCreateGroup(draftGroup)} className="w-full rounded-xl bg-slate-900 text-white py-2 font-semibold">Create Group</button>
            <div className="border-t pt-4 space-y-3">
              <h4 className="font-semibold text-slate-800">Create Center</h4>
              <input className="w-full p-2 border rounded-lg" placeholder="Center code" value={draftCenter.centerCode} onChange={e => setDraftCenter({ ...draftCenter, centerCode: e.target.value })} />
              <input className="w-full p-2 border rounded-lg" placeholder="Center name" value={draftCenter.centerName} onChange={e => setDraftCenter({ ...draftCenter, centerName: e.target.value })} />
              <button onClick={() => props.onCreateCenter(draftCenter)} className="w-full rounded-xl bg-emerald-600 text-white py-2 font-semibold">Create Center</button>
            </div>
          </section>

          <section className="xl:col-span-2 rounded-2xl bg-white border border-slate-200 p-5">
            <div className="flex items-center justify-between mb-4"><h3 className="font-bold text-slate-900">Lending Groups</h3><MapPinned className="text-slate-400" size={18} /></div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              {props.groups.map(group => (
                <div key={group.id} className="rounded-2xl border border-slate-200 p-4 bg-slate-50">
                  <div className="flex justify-between items-start gap-2">
                    <div>
                      <div className="font-bold text-slate-900">{group.groupName}</div>
                      <div className="text-xs text-slate-500">{group.groupCode || group.id}</div>
                    </div>
                    <div className="text-xs font-semibold uppercase text-slate-600">{group.status}</div>
                  </div>
                  <div className="mt-3 text-sm text-slate-600">{group.members.length} members � {group.meetingDayOfWeek} � {group.meetingLocation || 'No location'}</div>
                  <div className="mt-3 flex gap-2">
                    <select className="flex-1 p-2 border rounded-lg text-sm" value={selectedGroupId === group.id ? selectedCustomerId : ''} onChange={e => { setSelectedGroupId(group.id); setSelectedCustomerId(e.target.value); }}>
                      <option value="">Add member</option>
                      {props.customers.filter(c => !group.members.some(m => m.customerId === c.id)).map(customer => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
                    </select>
                    <button onClick={() => selectedCustomerId && props.onAddMember(group.id, { customerId: selectedCustomerId, memberRole: 'MEMBER' })} className="px-3 rounded-lg bg-white border border-slate-300 text-sm">Add</button>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>
      )}

      {activeTab === 'applications' && (
        <div className="grid grid-cols-1 xl:grid-cols-[360px_1fr] gap-5">
          <section className="rounded-2xl bg-white border border-slate-200 p-5 space-y-3">
            <h3 className="font-bold text-slate-900">New Cycle Application</h3>
            <select className="w-full p-2 border rounded-lg" value={applicationGroupId} onChange={e => setApplicationGroupId(e.target.value)}>
              <option value="">Select group</option>
              {props.groups.map(group => <option key={group.id} value={group.id}>{group.groupName}</option>)}
            </select>
            <select className="w-full p-2 border rounded-lg" value={applicationProductId} onChange={e => setApplicationProductId(e.target.value)}>
              <option value="">Select product</option>
              {groupLoanProducts.map(product => <option key={product.id} value={product.id}>{product.name}</option>)}
            </select>
            <button onClick={createCycleApplication} className="w-full rounded-xl bg-slate-900 text-white py-2 font-semibold">Create Draft Application</button>
          </section>
          <section className="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 className="font-bold text-slate-900 mb-4">Application Queue</h3>
            <div className="space-y-4 max-h-[560px] overflow-auto">
              {props.applications.map(app => (
                <div key={app.id} className="rounded-2xl border border-slate-200 p-4">
                  <div className="flex justify-between items-start gap-2">
                    <div>
                      <div className="font-bold text-slate-900">{app.groupName}</div>
                      <div className="text-sm text-slate-500">{app.productName} � Cycle {app.loanCycleNo}</div>
                    </div>
                    <div className="text-xs font-semibold uppercase text-slate-600">{app.status}</div>
                  </div>
                  <div className="mt-3 text-sm text-slate-600">Requested {app.totalRequestedAmount.toLocaleString()} � Approved {app.totalApprovedAmount.toLocaleString()}</div>
                  <div className="mt-3 flex flex-wrap gap-2">
                    <button onClick={() => props.onSubmitApplication(app.id)} className="px-3 py-1.5 rounded-lg border text-sm">Submit</button>
                    <button onClick={() => props.onApproveApplication(app.id)} className="px-3 py-1.5 rounded-lg bg-emerald-600 text-white text-sm">Approve</button>
                    <button onClick={() => props.onRejectApplication(app.id)} className="px-3 py-1.5 rounded-lg bg-rose-600 text-white text-sm">Reject</button>
                    <button onClick={() => props.onDisburseApplication(app.id)} className="px-3 py-1.5 rounded-lg bg-slate-900 text-white text-sm">Disburse</button>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>
      )}

      {activeTab === 'meetings' && (
        <div className="grid grid-cols-1 xl:grid-cols-[360px_1fr] gap-5">
          <section className="rounded-2xl bg-white border border-slate-200 p-5 space-y-3">
            <h3 className="font-bold text-slate-900">Create Meeting</h3>
            <select className="w-full p-2 border rounded-lg" value={draftMeeting.groupId} onChange={e => setDraftMeeting({ ...draftMeeting, groupId: e.target.value })}>
              <option value="">Select group</option>
              {props.groups.map(group => <option key={group.id} value={group.id}>{group.groupName}</option>)}
            </select>
            <input type="date" className="w-full p-2 border rounded-lg" value={draftMeeting.meetingDate} onChange={e => setDraftMeeting({ ...draftMeeting, meetingDate: e.target.value })} />
            <button onClick={() => props.onCreateMeeting(draftMeeting)} className="w-full rounded-xl bg-slate-900 text-white py-2 font-semibold">Create Meeting</button>
          </section>
          <section className="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 className="font-bold text-slate-900 mb-4">Meeting Calendar</h3>
            <div className="space-y-4 max-h-[560px] overflow-auto">
              {props.meetings.map(meeting => (
                <div key={meeting.id} className="rounded-2xl border border-slate-200 p-4 flex items-center justify-between gap-4">
                  <div>
                    <div className="font-bold text-slate-900">{meeting.groupName}</div>
                    <div className="text-sm text-slate-500">{meeting.meetingDate} � {meeting.meetingType} � {meeting.attendanceCount} attendance</div>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-slate-600"><HandCoins size={16} /> {meeting.status}</div>
                </div>
              ))}
            </div>
          </section>
        </div>
      )}

      {activeTab === 'reports' && (
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
          <section className="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 className="font-bold text-slate-900 mb-3">Cycle Analysis</h3>
            <div className="space-y-2 text-sm">{props.cycleAnalysis.map((item, idx) => <div key={idx} className="flex justify-between border-b pb-2"><span>Cycle {item.cycleNo}</span><span>{item.accounts} accounts</span></div>)}</div>
          </section>
          <section className="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 className="font-bold text-slate-900 mb-3">Delinquency Buckets</h3>
            <div className="space-y-2 text-sm">{props.delinquencyReport.map((item, idx) => <div key={idx} className="flex justify-between border-b pb-2"><span>{item.parBucket}</span><span>{Number(item.exposure || 0).toLocaleString()}</span></div>)}</div>
          </section>
          <section className="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 className="font-bold text-slate-900 mb-3">Meeting Collections</h3>
            <div className="space-y-2 text-sm">{props.meetingCollectionsReport.map((item, idx) => <div key={idx} className="flex justify-between border-b pb-2"><span>{item.collectionDate}</span><span>{Number(item.totalCollected || 0).toLocaleString()}</span></div>)}</div>
          </section>
        </div>
      )}
    </div>
  );
};

export default GroupLendingHub;
