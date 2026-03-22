
import React, { useState } from 'react';
import { DevTask } from '../types';
import { CheckCircle2, Circle, Clock, ArrowRight, ArrowLeft, Trash2, Plus, X } from 'lucide-react';

interface DevelopmentTasksProps {
    tasks?: DevTask[];
    onAddTask?: (task: Omit<DevTask, 'id' | 'status'>) => void;
    onUpdateStatus?: (id: string, status: DevTask['status']) => void;
    onDelete?: (id: string) => void;
}

const DevelopmentTasks: React.FC<DevelopmentTasksProps> = ({ 
    tasks = [], 
    onAddTask, 
    onUpdateStatus, 
    onDelete 
}) => {
    const [showAddModal, setShowAddModal] = useState(false);
    const [newTask, setNewTask] = useState({
        title: '',
        category: 'BACKEND' as DevTask['category'],
        priority: 'MEDIUM' as DevTask['priority']
    });

    const handleCreate = () => {
        if (!newTask.title) return;
        if (onAddTask) {
            onAddTask(newTask);
            setShowAddModal(false);
            setNewTask({ title: '', category: 'BACKEND', priority: 'MEDIUM' });
        }
    };

    const renderTask = (task: DevTask) => (
        <div key={task.id} className="bg-white p-4 rounded border border-gray-200 shadow-sm mb-3 hover:shadow-md transition-shadow group relative">
            <div className="flex justify-between items-start mb-2">
                <span className={`text-[10px] font-bold px-2 py-0.5 rounded ${
                    task.category === 'BACKEND' ? 'bg-purple-100 text-purple-700' :
                    task.category === 'FRONTEND' ? 'bg-blue-100 text-blue-700' :
                    'bg-orange-100 text-orange-700'
                }`}>
                    {task.category}
                </span>
                <span className={`text-[10px] font-bold ${
                    task.priority === 'HIGH' ? 'text-red-600 bg-red-50 px-1 rounded' :
                    task.priority === 'MEDIUM' ? 'text-yellow-600 bg-yellow-50 px-1 rounded' : 
                    'text-green-600 bg-green-50 px-1 rounded'
                }`}>
                    {task.priority}
                </span>
            </div>
            <p className="text-sm font-medium text-gray-800 mb-3">{task.title}</p>
            
            <div className="flex justify-between items-center pt-2 border-t border-gray-50 opacity-0 group-hover:opacity-100 transition-opacity">
                {onDelete && (
                    <button 
                        onClick={() => onDelete(task.id)}
                        className="text-gray-400 hover:text-red-500 p-1 rounded"
                        title="Delete Task"
                    >
                        <Trash2 size={14} />
                    </button>
                )}
                
                <div className="flex gap-1 ml-auto">
                    {task.status !== 'TODO' && onUpdateStatus && (
                        <button 
                            onClick={() => onUpdateStatus(task.id, task.status === 'DONE' ? 'IN_PROGRESS' : 'TODO')}
                            className="p-1 bg-gray-100 hover:bg-gray-200 rounded text-gray-600"
                            title="Move Back"
                        >
                            <ArrowLeft size={14} />
                        </button>
                    )}
                    {task.status !== 'DONE' && onUpdateStatus && (
                        <button 
                            onClick={() => onUpdateStatus(task.id, task.status === 'TODO' ? 'IN_PROGRESS' : 'DONE')}
                            className="p-1 bg-gray-100 hover:bg-gray-200 rounded text-gray-600"
                            title="Move Forward"
                        >
                            <ArrowRight size={14} />
                        </button>
                    )}
                </div>
            </div>
        </div>
    );

    return (
        <div className="flex flex-col h-full bg-gray-50 rounded-xl overflow-hidden relative">
             <div className="bg-white px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <div>
                    <h1 className="text-xl font-bold text-gray-900">Project Roadmap</h1>
                    <p className="text-gray-500 text-sm">Development tasks to reach Fineract/Mambu Parity</p>
                </div>
                <button 
                    onClick={() => setShowAddModal(true)}
                    className="bg-blue-600 text-white px-3 py-2 rounded-lg text-sm font-bold hover:bg-blue-700 flex items-center gap-2"
                >
                    <Plus size={16} /> Add Task
                </button>
            </div>
            
            <div className="flex-1 overflow-x-auto p-6">
                <div className="flex gap-6 h-full min-w-[800px]">
                    {/* TODO COL */}
                    <div className="flex-1 bg-gray-100 rounded-lg p-4 flex flex-col">
                        <div className="flex items-center gap-2 mb-4 text-gray-500 font-bold text-sm uppercase">
                            <Circle size={16} /> To Do
                            <span className="ml-auto bg-gray-200 text-gray-600 text-xs px-2 py-0.5 rounded-full">{tasks.filter(t => t.status === 'TODO').length}</span>
                        </div>
                        <div className="flex-1 overflow-y-auto">
                            {tasks.filter(t => t.status === 'TODO').map(renderTask)}
                        </div>
                    </div>

                    {/* IN PROGRESS COL */}
                    <div className="flex-1 bg-blue-50 rounded-lg p-4 flex flex-col border border-blue-100">
                         <div className="flex items-center gap-2 mb-4 text-blue-600 font-bold text-sm uppercase">
                            <Clock size={16} /> In Progress
                            <span className="ml-auto bg-blue-100 text-blue-600 text-xs px-2 py-0.5 rounded-full">{tasks.filter(t => t.status === 'IN_PROGRESS').length}</span>
                        </div>
                        <div className="flex-1 overflow-y-auto">
                            {tasks.filter(t => t.status === 'IN_PROGRESS').map(renderTask)}
                        </div>
                    </div>

                    {/* DONE COL */}
                    <div className="flex-1 bg-green-50 rounded-lg p-4 flex flex-col border border-green-100">
                         <div className="flex items-center gap-2 mb-4 text-green-600 font-bold text-sm uppercase">
                            <CheckCircle2 size={16} /> Done
                            <span className="ml-auto bg-green-100 text-green-600 text-xs px-2 py-0.5 rounded-full">{tasks.filter(t => t.status === 'DONE').length}</span>
                        </div>
                        <div className="flex-1 overflow-y-auto">
                            {tasks.filter(t => t.status === 'DONE').map(renderTask)}
                        </div>
                    </div>
                </div>
            </div>

            {/* ADD TASK MODAL */}
            {showAddModal && (
                <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4 backdrop-blur-sm">
                    <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
                        <div className="flex justify-between items-center mb-4">
                            <h3 className="font-bold text-lg text-gray-800">New Task</h3>
                            <button onClick={() => setShowAddModal(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={20} />
                            </button>
                        </div>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Title</label>
                                <input 
                                    type="text" 
                                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                                    value={newTask.title}
                                    onChange={e => setNewTask({...newTask, title: e.target.value})}
                                    autoFocus
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Category</label>
                                <select 
                                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm bg-white"
                                    value={newTask.category}
                                    onChange={e => setNewTask({...newTask, category: e.target.value as any})}
                                >
                                    <option value="BACKEND">Backend</option>
                                    <option value="FRONTEND">Frontend</option>
                                    <option value="DEVOPS">DevOps</option>
                                </select>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
                                <select 
                                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm bg-white"
                                    value={newTask.priority}
                                    onChange={e => setNewTask({...newTask, priority: e.target.value as any})}
                                >
                                    <option value="HIGH">High</option>
                                    <option value="MEDIUM">Medium</option>
                                    <option value="LOW">Low</option>
                                </select>
                            </div>
                            <button 
                                onClick={handleCreate}
                                className="w-full bg-blue-600 text-white font-bold py-2 rounded-lg hover:bg-blue-700 mt-2"
                            >
                                Create Task
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default DevelopmentTasks;
