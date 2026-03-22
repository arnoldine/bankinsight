import React, { useState, useEffect } from 'react';
import { GitBranch, Building, ChevronRight, ChevronDown, Plus, Trash2 } from 'lucide-react';

interface BranchHierarchy {
  id: number;
  branchId: string;
  branchCode: string;
  branchName: string;
  parentBranchId?: string;
  parentBranchCode?: string;
  parentBranchName?: string;
  level: number;
  path?: string;
  children: BranchHierarchy[];
}

export function BranchHierarchyTree() {
  const [tree, setTree] = useState<BranchHierarchy[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());
  const [showAddForm, setShowAddForm] = useState(false);
  const [branches, setBranches] = useState<any[]>([]);
  const [formData, setFormData] = useState({
    branchId: '',
    parentBranchId: ''
  });

  useEffect(() => {
    loadTree();
    loadBranches();
  }, []);

  const loadTree = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/branchhierarchy/tree', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setTree(await res.json());
      }
    } catch (error) {
      console.error('Failed to load branch tree:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadBranches = async () => {
    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/branches', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        setBranches(await res.json());
      }
    } catch (error) {
      console.error('Failed to load branches:', error);
    }
  };

  const toggleNode = (branchId: string) => {
    const newExpanded = new Set(expandedNodes);
    if (newExpanded.has(branchId)) {
      newExpanded.delete(branchId);
    } else {
      newExpanded.add(branchId);
    }
    setExpandedNodes(newExpanded);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch('http://localhost:5176/api/branchhierarchy', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData)
      });

      if (res.ok) {
        setShowAddForm(false);
        setFormData({ branchId: '', parentBranchId: '' });
        loadTree();
      }
    } catch (error) {
      console.error('Failed to create hierarchy:', error);
    }
  };

  const deleteHierarchy = async (id: number) => {
    if (!confirm('Are you sure you want to delete this hierarchy relationship?')) return;

    try {
      const token = localStorage.getItem('bankinsight_token');
      const res = await fetch(`http://localhost:5176/api/branchhierarchy/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (res.ok) {
        loadTree();
      }
    } catch (error) {
      console.error('Failed to delete hierarchy:', error);
    }
  };

  const renderNode = (node: BranchHierarchy, depth: number = 0) => {
    const isExpanded = expandedNodes.has(node.branchId);
    const hasChildren = node.children && node.children.length > 0;

    return (
      <div key={node.id} className="mb-2">
        <div
          className="flex items-center gap-2 p-3 rounded-lg hover:bg-gray-50 transition-colors cursor-pointer"
          style={{ marginLeft: `${depth * 24}px` }}
        >
          {hasChildren ? (
            <button
              onClick={() => toggleNode(node.branchId)}
              className="p-1 hover:bg-gray-200 rounded transition-colors"
            >
              {isExpanded ? (
                <ChevronDown className="w-4 h-4 text-gray-600" />
              ) : (
                <ChevronRight className="w-4 h-4 text-gray-600" />
              )}
            </button>
          ) : (
            <div className="w-6" />
          )}

          <Building className="w-5 h-5 text-indigo-600" />
          <div className="flex-1">
            <div className="font-medium text-gray-900">{node.branchName}</div>
            <div className="text-sm text-gray-500">
              {node.branchCode} • Level {node.level}
            </div>
          </div>

          <button
            onClick={() => deleteHierarchy(node.id)}
            className="p-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>

        {hasChildren && isExpanded && (
          <div>
            {node.children.map((child) => renderNode(child, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-500">Loading branch hierarchy...</div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <GitBranch className="w-6 h-6 text-indigo-600" />
          <h2 className="text-xl font-semibold text-gray-900">Branch Hierarchy</h2>
        </div>
        <button
          onClick={() => setShowAddForm(!showAddForm)}
          className="flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
          <Plus className="w-5 h-5" />
          Add Relationship
        </button>
      </div>

      {showAddForm && (
        <form onSubmit={handleSubmit} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Branch
              </label>
              <select
                value={formData.branchId}
                onChange={(e) => setFormData({ ...formData, branchId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                required
              >
                <option value="">Select branch...</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name} ({branch.code})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Parent Branch (Optional)
              </label>
              <select
                value={formData.parentBranchId}
                onChange={(e) => setFormData({ ...formData, parentBranchId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                <option value="">None (Root)</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name} ({branch.code})
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setShowAddForm(false)}
              className="px-4 py-2 text-gray-600 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
            >
              Create
            </button>
          </div>
        </form>
      )}

      <div className="space-y-1">
        {tree.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            No branch hierarchies defined
          </div>
        ) : (
          tree.map((node) => renderNode(node))
        )}
      </div>
    </div>
  );
}
