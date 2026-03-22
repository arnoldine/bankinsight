
import React, { useState, useMemo } from 'react';
import { Group, Customer } from '../types';
import { Users, Plus, Search, MapPin, Calendar, UserPlus, X, Trash2, ArrowRight } from 'lucide-react';

interface GroupManagerProps {
    groups: Group[];
    customers: Customer[];
    onCreateGroup: (group: Omit<Group, 'id' | 'formationDate'>) => void;
    onAddMember: (groupId: string, cifId: string) => void;
    onRemoveMember: (groupId: string, cifId: string) => void;
}

const GroupManager: React.FC<GroupManagerProps> = ({ groups, customers, onCreateGroup, onAddMember, onRemoveMember }) => {
    const [view, setView] = useState<'LIST' | 'CREATE' | 'DETAILS'>('LIST');
    const [selectedGroup, setSelectedGroup] = useState<Group | null>(null);
    const [searchTerm, setSearchTerm] = useState('');
    
    // Create Form State
    const [newGroup, setNewGroup] = useState<Partial<Group>>({
        name: '',
        officer: 'STF001', // Default
        meetingDay: 'Monday',
        status: 'ACTIVE',
        members: []
    });

    // Add Member State
    const [memberSearch, setMemberSearch] = useState('');

    const filteredGroups = useMemo(() => {
        return groups.filter(g => g.name.toLowerCase().includes(searchTerm.toLowerCase()) || g.id.toLowerCase().includes(searchTerm.toLowerCase()));
    }, [groups, searchTerm]);

    const handleCreate = () => {
        if (!newGroup.name) return;
        onCreateGroup(newGroup as any);
        setView('LIST');
        setNewGroup({ name: '', officer: 'STF001', meetingDay: 'Monday', status: 'ACTIVE', members: [] });
    };

    const searchResults = useMemo(() => {
        if (!memberSearch) return [];
        return customers.filter(c => 
            (c.name.toLowerCase().includes(memberSearch.toLowerCase()) || c.id.toLowerCase().includes(memberSearch.toLowerCase())) &&
            !selectedGroup?.members.includes(c.id) // Exclude existing members
        ).slice(0, 5);
    }, [customers, memberSearch, selectedGroup]);

    return (
        <div className="flex flex-col h-full bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            {/* Header */}
            <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                <h2 className="text-lg font-bold text-gray-800 flex items-center gap-2">
                    <Users className="text-purple-600" size={20} /> Group Management
                </h2>
                {view === 'LIST' && (
                    <button onClick={() => setView('CREATE')} className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-purple-700 flex items-center gap-2">
                        <Plus size={16} /> Create Group
                    </button>
                )}
                {view !== 'LIST' && (
                    <button onClick={() => setView('LIST')} className="text-gray-600 hover:text-purple-600 text-sm font-medium px-3 py-2 border rounded bg-white">
                        &larr; Back to List
                    </button>
                )}
            </div>

            <div className="flex-1 overflow-hidden p-6">
                
                {/* LIST VIEW */}
                {view === 'LIST' && (
                    <div className="h-full flex flex-col">
                        <div className="mb-4 relative max-w-md">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                            <input 
                                type="text" 
                                placeholder="Search groups..." 
                                value={searchTerm}
                                onChange={e => setSearchTerm(e.target.value)}
                                className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-purple-500 outline-none"
                            />
                        </div>
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 overflow-y-auto pb-4">
                            {filteredGroups.map(group => (
                                <div key={group.id} className="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow cursor-pointer" onClick={() => { setSelectedGroup(group); setView('DETAILS'); }}>
                                    <div className="flex justify-between items-start mb-2">
                                        <div className="flex items-center gap-3">
                                            <div className="w-10 h-10 bg-purple-100 text-purple-600 rounded-full flex items-center justify-center font-bold">
                                                {group.name.charAt(0)}
                                            </div>
                                            <div>
                                                <h3 className="font-bold text-gray-800 text-sm">{group.name}</h3>
                                                <p className="text-xs text-gray-500">{group.id}</p>
                                            </div>
                                        </div>
                                        <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${group.status === 'ACTIVE' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                                            {group.status}
                                        </span>
                                    </div>
                                    <div className="flex items-center gap-4 text-xs text-gray-500 mt-4 pt-4 border-t border-gray-100">
                                        <span className="flex items-center gap-1"><Users size={12}/> {group.members.length} Members</span>
                                        <span className="flex items-center gap-1"><Calendar size={12}/> {group.meetingDay}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* CREATE VIEW */}
                {view === 'CREATE' && (
                    <div className="max-w-2xl mx-auto bg-white border border-gray-200 rounded-lg shadow-sm p-8">
                        <h3 className="text-xl font-bold text-gray-800 mb-6">New Lending Group</h3>
                        <div className="grid grid-cols-2 gap-6">
                            <div className="col-span-2">
                                <label className="block text-sm font-bold text-gray-700 mb-1">Group Name</label>
                                <input type="text" className="w-full border rounded p-2.5 outline-none focus:ring-2 focus:ring-purple-500" value={newGroup.name} onChange={e => setNewGroup({...newGroup, name: e.target.value})} />
                            </div>
                            <div>
                                <label className="block text-sm font-bold text-gray-700 mb-1">Meeting Day</label>
                                <select className="w-full border rounded p-2.5 bg-white" value={newGroup.meetingDay} onChange={e => setNewGroup({...newGroup, meetingDay: e.target.value})}>
                                    {['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'].map(d => <option key={d} value={d}>{d}</option>)}
                                </select>
                            </div>
                            <div>
                                <label className="block text-sm font-bold text-gray-700 mb-1">Credit Officer ID</label>
                                <input type="text" className="w-full border rounded p-2.5 outline-none" value={newGroup.officer} onChange={e => setNewGroup({...newGroup, officer: e.target.value})} />
                            </div>
                        </div>
                        <div className="mt-8 flex justify-end gap-3">
                            <button onClick={() => setView('LIST')} className="px-4 py-2 text-gray-600 font-medium hover:bg-gray-100 rounded">Cancel</button>
                            <button onClick={handleCreate} className="px-6 py-2 bg-purple-600 text-white font-bold rounded hover:bg-purple-700">Create Group</button>
                        </div>
                    </div>
                )}

                {/* DETAILS VIEW */}
                {view === 'DETAILS' && selectedGroup && (
                    <div className="flex h-full gap-6">
                        {/* Group Info */}
                        <div className="w-1/3 space-y-6">
                            <div className="bg-white border border-gray-200 rounded-lg p-6">
                                <div className="text-center mb-6">
                                    <div className="w-16 h-16 bg-purple-100 text-purple-600 rounded-full flex items-center justify-center text-2xl font-bold mx-auto mb-2">
                                        {selectedGroup.name.charAt(0)}
                                    </div>
                                    <h3 className="font-bold text-gray-900 text-lg">{selectedGroup.name}</h3>
                                    <p className="text-sm text-gray-500">{selectedGroup.id}</p>
                                </div>
                                <div className="space-y-3 text-sm">
                                    <div className="flex justify-between py-2 border-b border-gray-100">
                                        <span className="text-gray-500">Status</span>
                                        <span className="font-bold text-green-600">{selectedGroup.status}</span>
                                    </div>
                                    <div className="flex justify-between py-2 border-b border-gray-100">
                                        <span className="text-gray-500">Officer</span>
                                        <span className="font-medium text-gray-800">{selectedGroup.officer}</span>
                                    </div>
                                    <div className="flex justify-between py-2 border-b border-gray-100">
                                        <span className="text-gray-500">Meeting Day</span>
                                        <span className="font-medium text-gray-800">{selectedGroup.meetingDay}</span>
                                    </div>
                                    <div className="flex justify-between py-2 border-b border-gray-100">
                                        <span className="text-gray-500">Formed</span>
                                        <span className="font-medium text-gray-800">{selectedGroup.formationDate}</span>
                                    </div>
                                </div>
                            </div>
                            
                            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                                <h4 className="font-bold text-blue-900 text-sm mb-2">Add Member</h4>
                                <div className="relative">
                                    <input 
                                        type="text" 
                                        placeholder="Search Customer Name or CIF..." 
                                        className="w-full border border-blue-200 rounded p-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                                        value={memberSearch}
                                        onChange={e => setMemberSearch(e.target.value)}
                                    />
                                    {searchResults.length > 0 && (
                                        <div className="absolute top-full left-0 w-full bg-white border border-gray-200 rounded shadow-lg mt-1 z-10 max-h-48 overflow-y-auto">
                                            {searchResults.map(c => (
                                                <button 
                                                    key={c.id} 
                                                    className="w-full text-left p-2 hover:bg-gray-50 text-sm flex justify-between items-center"
                                                    onClick={() => { onAddMember(selectedGroup.id, c.id); setMemberSearch(''); }}
                                                >
                                                    <div>
                                                        <div className="font-bold text-gray-800">{c.name}</div>
                                                        <div className="text-xs text-gray-500">{c.id}</div>
                                                    </div>
                                                    <Plus size={14} className="text-blue-600"/>
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* Members List */}
                        <div className="flex-1 bg-white border border-gray-200 rounded-lg flex flex-col">
                            <div className="p-4 border-b border-gray-200 bg-gray-50 flex justify-between items-center">
                                <h3 className="font-bold text-gray-800">Group Members ({selectedGroup.members.length})</h3>
                            </div>
                            <div className="flex-1 overflow-y-auto">
                                <table className="w-full text-sm text-left">
                                    <thead className="bg-gray-50 text-gray-500 font-medium">
                                        <tr>
                                            <th className="p-3">Member Name</th>
                                            <th className="p-3">CIF</th>
                                            <th className="p-3">Role</th>
                                            <th className="p-3 text-right">Action</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-100">
                                        {selectedGroup.members.map((cif, index) => {
                                            const cust = customers.find(c => c.id === cif);
                                            return (
                                                <tr key={cif} className="hover:bg-gray-50">
                                                    <td className="p-3 font-medium text-gray-900">
                                                        {cust?.name || 'Unknown Client'}
                                                    </td>
                                                    <td className="p-3 font-mono text-gray-500">{cif}</td>
                                                    <td className="p-3">
                                                        {index === 0 ? <span className="bg-purple-100 text-purple-700 text-xs px-2 py-0.5 rounded font-bold">Chairperson</span> : <span className="text-gray-500 text-xs">Member</span>}
                                                    </td>
                                                    <td className="p-3 text-right">
                                                        <button 
                                                            onClick={() => onRemoveMember(selectedGroup.id, cif)}
                                                            className="text-red-400 hover:text-red-600 p-1 rounded hover:bg-red-50"
                                                        >
                                                            <Trash2 size={16} />
                                                        </button>
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                        {selectedGroup.members.length === 0 && (
                                            <tr><td colSpan={4} className="p-8 text-center text-gray-400">No members assigned to this group.</td></tr>
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default GroupManager;
