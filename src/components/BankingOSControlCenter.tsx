import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  Boxes,
  Brush,
  Database,
  FileStack,
  Palette,
  RefreshCw,
  Send,
  Workflow
} from 'lucide-react';
import {
  BankingOSProcessCatalogItem,
  BankingOSLaunchContext,
  BankingOSFormCatalogItem,
  bankingOSService,
  BankingOSProductConfiguration,
  BankingOSPublishBundle,
  BankingOSProcessPack,
  BankingOSProcessSummary,
  BankingOSProcessTask,
  BankingOSSeedField,
  BankingOSSeedForm,
  BankingOSTaskContext,
  BankingOSThemeCatalogItem,
  BankingOSTheme
} from '../services/bankingOSService';

export default function BankingOSControlCenter() {
  const [processPack, setProcessPack] = useState<BankingOSProcessPack | null>(null);
  const [forms, setForms] = useState<BankingOSSeedForm[]>([]);
  const [themes, setThemes] = useState<BankingOSTheme[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedProcessCode, setSelectedProcessCode] = useState<string | null>(null);
  const [selectedThemeCode, setSelectedThemeCode] = useState<string | null>(null);
  const [launchContext, setLaunchContext] = useState<BankingOSLaunchContext | null>(null);
  const [runnerValues, setRunnerValues] = useState<Record<string, string>>({});
  const [launching, setLaunching] = useState(false);
  const [launchResult, setLaunchResult] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [myTasks, setMyTasks] = useState<BankingOSProcessTask[]>([]);
  const [claimableTasks, setClaimableTasks] = useState<BankingOSProcessTask[]>([]);
  const [publishBundles, setPublishBundles] = useState<BankingOSPublishBundle[]>([]);
  const [products, setProducts] = useState<BankingOSProductConfiguration[]>([]);
  const [formCatalog, setFormCatalog] = useState<BankingOSFormCatalogItem[]>([]);
  const [processCatalog, setProcessCatalog] = useState<BankingOSProcessCatalogItem[]>([]);
  const [themeCatalog, setThemeCatalog] = useState<BankingOSThemeCatalogItem[]>([]);
  const [taskActionStatus, setTaskActionStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [processReleaseStatus, setProcessReleaseStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [formReleaseStatus, setFormReleaseStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [themeReleaseStatus, setThemeReleaseStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [bundleReleaseStatus, setBundleReleaseStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [productReleaseStatus, setProductReleaseStatus] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);
  const [productEditor, setProductEditor] = useState<BankingOSProductConfiguration | null>(null);
  const [processEditor, setProcessEditor] = useState<BankingOSProcessSummary | null>(null);
  const [selectedRuntimeTaskId, setSelectedRuntimeTaskId] = useState<string | null>(null);
  const [taskRunnerValues, setTaskRunnerValues] = useState<Record<string, string>>({});
  const [taskContext, setTaskContext] = useState<BankingOSTaskContext | null>(null);
  const [taskRemarks, setTaskRemarks] = useState('');
  const [themeEditorName, setThemeEditorName] = useState('');
  const [themeEditorTokens, setThemeEditorTokens] = useState<Record<string, string>>({});

  const loadMetadata = async () => {
    setLoading(true);
    setError(null);

    try {
      const [processPackResponse, formsResponse, themesResponse, myTasksResponse, claimableTasksResponse, publishBundlesResponse, formCatalogResponse, processCatalogResponse, themeCatalogResponse, productsResponse] = await Promise.all([
        bankingOSService.getProcessPack(),
        bankingOSService.getForms(),
        bankingOSService.getThemes(),
        bankingOSService.getMyTasks(),
        bankingOSService.getClaimableTasks().catch(() => []),
        bankingOSService.getPublishBundles(),
        bankingOSService.getFormCatalog(),
        bankingOSService.getProcessCatalog(),
        bankingOSService.getThemeCatalog(),
        bankingOSService.getProducts()
      ]);

      setProcessPack(processPackResponse);
      setForms(formsResponse);
      setThemes(themesResponse.themes || []);
      setMyTasks(myTasksResponse || []);
      setClaimableTasks(claimableTasksResponse || []);
      setPublishBundles(publishBundlesResponse || []);
      setProducts(productsResponse || []);
      setFormCatalog(formCatalogResponse || []);
      setProcessCatalog(processCatalogResponse || []);
      setThemeCatalog(themeCatalogResponse || []);
      setSelectedProcessCode((current) => current || processPackResponse.processes[0]?.code || null);
      setSelectedThemeCode((current) => current || themesResponse.themes?.[0]?.code || null);
      setSelectedProductId((current) => current || productsResponse[0]?.id || null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load BankingOS metadata.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMetadata();
  }, []);

  const stats = useMemo(() => {
    const processCount = processPack?.processes.length || 0;
    const stageCount = processPack?.processes.reduce((sum, process) => sum + process.stages.length, 0) || 0;
    return [
      { label: 'Processes', value: String(processCount), icon: <Workflow className="h-5 w-5 text-brand-600 dark:text-brand-300" /> },
      { label: 'Stages', value: String(stageCount), icon: <Boxes className="h-5 w-5 text-emerald-600 dark:text-emerald-300" /> },
      { label: 'Forms', value: String(forms.length), icon: <Database className="h-5 w-5 text-blue-600 dark:text-blue-300" /> },
      { label: 'Themes', value: String(themes.length), icon: <Brush className="h-5 w-5 text-amber-600 dark:text-amber-300" /> },
    ];
  }, [forms.length, processPack, themes.length]);

  const selectedProcess = useMemo(
    () => processPack?.processes.find((process) => process.code === selectedProcessCode) || processPack?.processes[0] || null,
    [processPack, selectedProcessCode]
  );

  const selectedTheme = useMemo(
    () => themes.find((theme) => theme.code === selectedThemeCode) || themes[0] || null,
    [selectedThemeCode, themes]
  );

  const selectedProduct = useMemo(
    () => {
      if (selectedProductId) {
        return products.find((product) => product.id === selectedProductId) || null;
      }

      return products[0] || null;
    },
    [products, selectedProductId]
  );

  const linkedForms = useMemo(() => {
    if (launchContext?.primaryForm) {
      return [launchContext.primaryForm];
    }

    if (!selectedProcess) {
      return [];
    }

    const formCodes = selectedProcess.stages
      .map((stage) => stage.formCode)
      .filter((value): value is string => Boolean(value));

    return forms.filter((form) => formCodes.includes(form.code));
  }, [forms, launchContext?.primaryForm, selectedProcess]);

  const primaryLinkedForm = linkedForms[0] || null;

  useEffect(() => {
    if (!primaryLinkedForm) {
      setRunnerValues({});
      return;
    }

    const nextValues = primaryLinkedForm.fields.reduce<Record<string, string>>((accumulator, field) => {
      accumulator[field.id] = '';
      return accumulator;
    }, {});
    setRunnerValues(nextValues);
    setLaunchResult(null);
    setTaskActionStatus(null);
    setProcessReleaseStatus(null);
    setFormReleaseStatus(null);
    setBundleReleaseStatus(null);
  }, [primaryLinkedForm?.code]);

  useEffect(() => {
    if (!selectedProcess?.code) {
      setLaunchContext(null);
      return;
    }

    let isCancelled = false;

    const loadLaunchContext = async () => {
      try {
        const response = await bankingOSService.getLaunchContext(selectedProcess.code);
        if (!isCancelled) {
          setLaunchContext(response);
        }
      } catch {
        if (!isCancelled) {
          setLaunchContext(null);
        }
      }
    };

    loadLaunchContext();

    return () => {
      isCancelled = true;
    };
  }, [selectedProcess?.code]);

  useEffect(() => {
    setSelectedRuntimeTaskId(null);
    setTaskRunnerValues({});
    setTaskContext(null);
  }, [selectedProcess?.code]);

  useEffect(() => {
    if (!selectedProcess) {
      setProcessEditor(null);
      return;
    }

    setProcessEditor(JSON.parse(JSON.stringify(selectedProcess)) as BankingOSProcessSummary);
    setProcessReleaseStatus(null);
  }, [selectedProcess?.code]);

  useEffect(() => {
    if (!selectedTheme) {
      setThemeEditorName('');
      setThemeEditorTokens({});
      return;
    }

    setThemeEditorName(selectedTheme.name);
    setThemeEditorTokens(selectedTheme.tokens);
    setThemeReleaseStatus(null);
  }, [selectedTheme?.code]);

  useEffect(() => {
    if (!selectedProduct) {
      if (productEditor?.id !== selectedProductId) {
        setProductEditor(null);
      }
      return;
    }

    setProductEditor(JSON.parse(JSON.stringify(selectedProduct)) as BankingOSProductConfiguration);
    setProductReleaseStatus(null);
  }, [productEditor?.id, selectedProduct?.id, selectedProductId]);

  const resolveBundleActor = () => {
    return processPack?.productName ? 'bankingos-admin' : 'system';
  };

  const updateRunnerValue = (fieldId: string, value: string) => {
    setRunnerValues((current) => ({ ...current, [fieldId]: value }));
  };

  const handleLaunchProcess = async () => {
    if (!selectedProcess || !primaryLinkedForm) {
      return;
    }

    const firstMissingRequiredField = primaryLinkedForm.fields.find((field) => field.required && !String(runnerValues[field.id] || '').trim());
    if (firstMissingRequiredField) {
      setLaunchResult({
        tone: 'error',
        message: `${firstMissingRequiredField.label} is required before you can launch ${selectedProcess.name}.`
      });
      return;
    }

    setLaunching(true);
    setLaunchResult(null);

    try {
      const entityId = `BOS-${selectedProcess.code}-${Date.now()}`;
      const response = await bankingOSService.launchProcess(selectedProcess.code, {
        entityType: launchContext?.entityType || selectedProcess.entityType,
        entityId,
        correlationId: `${selectedProcess.code}-${Date.now()}`,
        payloadJson: JSON.stringify({
          source: 'BankingOSControlCenter',
          processCode: selectedProcess.code,
          formCode: primaryLinkedForm.code,
          submittedAt: new Date().toISOString(),
          data: runnerValues
        })
      });

      setLaunchResult({
        tone: 'success',
        message: `${selectedProcess.name} launched successfully. Instance ${response.instanceId} is now ${response.status}.`
      });
      await loadMetadata();
    } catch (err) {
      setLaunchResult({
        tone: 'error',
        message: err instanceof Error ? err.message : `Failed to launch ${selectedProcess.name}.`
      });
    } finally {
      setLaunching(false);
    }
  };

  const processTasks = useMemo(() => {
    if (!selectedProcess) {
      return { mine: [], claimable: [] };
    }

    const matchesSelectedProcess = (task: BankingOSProcessTask) =>
      task.processInstance?.processDefinitionVersion?.processDefinition?.code === selectedProcess.code;

    return {
      mine: myTasks.filter(matchesSelectedProcess),
      claimable: claimableTasks.filter(matchesSelectedProcess)
    };
  }, [claimableTasks, myTasks, selectedProcess]);

  const allVisibleTasks = useMemo(
    () => [...processTasks.mine, ...processTasks.claimable],
    [processTasks.claimable, processTasks.mine]
  );

  const selectedRuntimeTask = useMemo(
    () => allVisibleTasks.find((task) => task.id === selectedRuntimeTaskId) || null,
    [allVisibleTasks, selectedRuntimeTaskId]
  );

  const selectedRuntimeStage = taskContext?.stage || null;
  const selectedRuntimeForm = taskContext?.form || null;
  const runtimeFieldMap = useMemo(
    () => new Map((selectedRuntimeForm?.fields || []).map((field) => [field.id, field])),
    [selectedRuntimeForm]
  );
  const validationRules = taskContext?.validationRules || [];
  const taskActions = taskContext?.actions || [];
  const screenSections = taskContext?.screen?.sections || [];

  useEffect(() => {
    if (!selectedRuntimeForm) {
      setTaskRunnerValues({});
      return;
    }

    setTaskRunnerValues(
      selectedRuntimeForm.fields.reduce<Record<string, string>>((accumulator, field) => {
        accumulator[field.id] = '';
        return accumulator;
      }, {})
    );
  }, [selectedRuntimeForm?.code, selectedRuntimeTaskId]);

  useEffect(() => {
    setTaskRemarks('');
  }, [selectedRuntimeTaskId]);

  useEffect(() => {
    if (!selectedRuntimeTaskId) {
      setTaskContext(null);
      return;
    }

    let isCancelled = false;

    const loadTaskContext = async () => {
      try {
        const response = await bankingOSService.getTaskContext(selectedRuntimeTaskId);
        if (!isCancelled) {
          setTaskContext(response);
        }
      } catch (err) {
        if (!isCancelled) {
          setTaskContext(null);
          setTaskActionStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to load task context.' });
        }
      }
    };

    loadTaskContext();

    return () => {
      isCancelled = true;
    };
  }, [selectedRuntimeTaskId]);

  const handleClaimTask = async (taskId: string) => {
    try {
      setTaskActionStatus(null);
      await bankingOSService.claimTask(taskId);
      setTaskActionStatus({ tone: 'success', message: 'Task claimed successfully.' });
      setSelectedRuntimeTaskId(taskId);
      await loadMetadata();
    } catch (err) {
      setTaskActionStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to claim task.' });
    }
  };

  const handleCompleteTask = async (task: BankingOSProcessTask, payloadJson?: string) => {
    const stepType = task.processStepDefinition?.stepType;
    const outcome = stepType === 'ApprovalTask' ? 'Approve' : 'Complete';

    try {
      setTaskActionStatus(null);
      await bankingOSService.completeTask(task.id, outcome, payloadJson ?? JSON.stringify({ source: 'BankingOSControlCenter' }));
      setTaskActionStatus({ tone: 'success', message: `${task.processStepDefinition?.stepName || 'Task'} completed with outcome ${outcome}.` });
      setSelectedRuntimeTaskId(null);
      await loadMetadata();
    } catch (err) {
      setTaskActionStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to complete task.' });
    }
  };

  const handleRejectTask = async (taskId: string, payloadJson?: string) => {
    try {
      setTaskActionStatus(null);
      await bankingOSService.rejectTask(taskId, payloadJson ?? JSON.stringify({ source: 'BankingOSControlCenter' }));
      setTaskActionStatus({ tone: 'success', message: 'Task rejected successfully.' });
      setSelectedRuntimeTaskId(null);
      await loadMetadata();
    } catch (err) {
      setTaskActionStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to reject task.' });
    }
  };

  const updateTaskRunnerValue = (fieldId: string, value: string) => {
    setTaskRunnerValues((current) => ({ ...current, [fieldId]: value }));
  };

  const buildTaskPayload = () => JSON.stringify({
    source: 'BankingOSControlCenter',
    taskId: selectedRuntimeTask?.id,
    stepCode: taskContext?.stepCode || selectedRuntimeTask?.processStepDefinition?.stepCode,
    stageCode: selectedRuntimeStage?.stageCode,
    remarks: taskRemarks,
    data: taskRunnerValues
  });

  const handleWorkbenchAction = async (actionCode: string) => {
    if (!selectedRuntimeTask) {
      return;
    }

    try {
      setTaskActionStatus(null);
      if (actionCode === 'claim') {
        await bankingOSService.claimTask(selectedRuntimeTask.id);
        setTaskActionStatus({ tone: 'success', message: `${taskContext?.stepName || 'Task'} claimed successfully.` });
      } else if (actionCode === 'reject') {
        await bankingOSService.rejectWorkbenchTask(selectedRuntimeTask.id, buildTaskPayload(), taskRemarks);
        setTaskActionStatus({ tone: 'success', message: `${taskContext?.stepName || 'Task'} rejected successfully through the BankingOS runtime contract.` });
      } else {
        await bankingOSService.completeWorkbenchTask(selectedRuntimeTask.id, buildTaskPayload(), taskRemarks);
        setTaskActionStatus({
          tone: 'success',
          message: `${taskContext?.stepName || 'Task'} ${actionCode === 'approve' ? 'approved' : 'completed'} successfully through the BankingOS runtime contract.`
        });
      }

      setSelectedRuntimeTaskId(null);
      await loadMetadata();
    } catch (err) {
      setTaskActionStatus({ tone: 'error', message: err instanceof Error ? err.message : `Failed to run action '${actionCode}' from workbench.` });
    }
  };

  const handleSaveLinkedFormDraft = async () => {
    if (!primaryLinkedForm) {
      return;
    }

    try {
      setFormReleaseStatus(null);
      await bankingOSService.saveFormDraft(primaryLinkedForm);
      setFormReleaseStatus({ tone: 'success', message: `${primaryLinkedForm.name} saved to BankingOS config as a draft version.` });
      await loadMetadata();
    } catch (err) {
      setFormReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to save form draft.' });
    }
  };

  const handlePublishLinkedForm = async () => {
    if (!primaryLinkedForm) {
      return;
    }

    try {
      setFormReleaseStatus(null);
      await bankingOSService.publishForm(primaryLinkedForm.code);
      setFormReleaseStatus({ tone: 'success', message: `${primaryLinkedForm.name} is now marked as published in BankingOS config.` });
      await loadMetadata();
    } catch (err) {
      setFormReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to publish form.' });
    }
  };

  const updateProcessField = (field: keyof Pick<BankingOSProcessSummary, 'name' | 'module' | 'entityType' | 'triggerType'>, value: string) => {
    setProcessEditor((current) => current ? { ...current, [field]: value } : current);
  };

  const updateProcessStageField = (
    stageIndex: number,
    field: keyof BankingOSProcessSummary['stages'][number],
    value: string | string[]
  ) => {
    setProcessEditor((current) => {
      if (!current) {
        return current;
      }

      const nextStages = current.stages.map((stage, index) =>
        index === stageIndex ? { ...stage, [field]: value } : stage
      );

      return {
        ...current,
        stages: nextStages
      };
    });
  };

  const updateProcessStageScreenField = (
    stageIndex: number,
    field: 'title' | 'description' | 'bannerMessage' | 'bannerTone',
    value: string
  ) => {
    setProcessEditor((current) => {
      if (!current) {
        return current;
      }

      const nextStages = current.stages.map((stage, index) => {
        if (index !== stageIndex) {
          return stage;
        }

        const nextScreen = {
          title: stage.screen?.title || stage.displayName,
          description: stage.screen?.description || '',
          bannerTone: stage.screen?.bannerTone || 'neutral',
          bannerMessage: stage.screen?.bannerMessage || '',
          sections: stage.screen?.sections || []
        };

        return {
          ...stage,
          screen: {
            ...nextScreen,
            [field]: value
          }
        };
      });

      return {
        ...current,
        stages: nextStages
      };
    });
  };

  const addProcessStageScreenSection = (stageIndex: number) => {
    setProcessEditor((current) => {
      if (!current) {
        return current;
      }

      const nextStages = current.stages.map((stage, index) => {
        if (index !== stageIndex) {
          return stage;
        }

        const nextScreen = {
          title: stage.screen?.title || stage.displayName,
          description: stage.screen?.description || '',
          bannerTone: stage.screen?.bannerTone || 'neutral',
          bannerMessage: stage.screen?.bannerMessage || '',
          sections: stage.screen?.sections || []
        };

        const nextSectionIndex = nextScreen.sections.length + 1;

        return {
          ...stage,
          screen: {
            ...nextScreen,
            sections: [
              ...nextScreen.sections,
              {
                id: `${stage.stageCode}-section-${nextSectionIndex}`,
                title: `Section ${nextSectionIndex}`,
                kind: 'form',
                fieldIds: []
              }
            ]
          }
        };
      });

      return {
        ...current,
        stages: nextStages
      };
    });
  };

  const updateProcessStageScreenSection = (
    stageIndex: number,
    sectionIndex: number,
    field: 'id' | 'title' | 'kind' | 'fieldIds',
    value: string | string[]
  ) => {
    setProcessEditor((current) => {
      if (!current) {
        return current;
      }

      const nextStages = current.stages.map((stage, index) => {
        if (index !== stageIndex) {
          return stage;
        }

        const nextScreen = {
          title: stage.screen?.title || stage.displayName,
          description: stage.screen?.description || '',
          bannerTone: stage.screen?.bannerTone || 'neutral',
          bannerMessage: stage.screen?.bannerMessage || '',
          sections: stage.screen?.sections || []
        };

        const nextSections = nextScreen.sections.map((section, currentSectionIndex) =>
          currentSectionIndex === sectionIndex ? { ...section, [field]: value } : section
        );

        return {
          ...stage,
          screen: {
            ...nextScreen,
            sections: nextSections
          }
        };
      });

      return {
        ...current,
        stages: nextStages
      };
    });
  };

  const removeProcessStageScreenSection = (stageIndex: number, sectionIndex: number) => {
    setProcessEditor((current) => {
      if (!current) {
        return current;
      }

      const nextStages = current.stages.map((stage, index) => {
        if (index !== stageIndex) {
          return stage;
        }

        const nextScreen = {
          title: stage.screen?.title || stage.displayName,
          description: stage.screen?.description || '',
          bannerTone: stage.screen?.bannerTone || 'neutral',
          bannerMessage: stage.screen?.bannerMessage || '',
          sections: stage.screen?.sections || []
        };

        return {
          ...stage,
          screen: {
            ...nextScreen,
            sections: nextScreen.sections.filter((_, currentSectionIndex) => currentSectionIndex !== sectionIndex)
          }
        };
      });

      return {
        ...current,
        stages: nextStages
      };
    });
  };

  const handleSaveProcessDraft = async () => {
    if (!processEditor) {
      return;
    }

    try {
      setProcessReleaseStatus(null);
      await bankingOSService.saveProcessDraft(processEditor);
      setProcessReleaseStatus({ tone: 'success', message: `${processEditor.name} saved as a BankingOS process draft.` });
      await loadMetadata();
    } catch (err) {
      setProcessReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to save process draft.' });
    }
  };

  const handlePublishProcess = async () => {
    if (!processEditor) {
      return;
    }

    try {
      setProcessReleaseStatus(null);
      await bankingOSService.saveProcessDraft(processEditor);
      await bankingOSService.publishProcess(processEditor.code);
      setProcessReleaseStatus({ tone: 'success', message: `${processEditor.name} is now published in BankingOS config.` });
      await loadMetadata();
    } catch (err) {
      setProcessReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to publish process.' });
    }
  };

  const updateThemeToken = (tokenKey: string, value: string) => {
    setThemeEditorTokens((current) => ({
      ...current,
      [tokenKey]: value
    }));
  };

  const handleSaveThemeDraft = async () => {
    if (!selectedTheme) {
      return;
    }

    try {
      setThemeReleaseStatus(null);
      await bankingOSService.saveThemeDraft({
        ...selectedTheme,
        name: themeEditorName,
        tokens: themeEditorTokens
      });
      setThemeReleaseStatus({ tone: 'success', message: `${themeEditorName} saved as a BankingOS draft theme version.` });
      await loadMetadata();
    } catch (err) {
      setThemeReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to save theme draft.' });
    }
  };

  const handlePublishTheme = async () => {
    if (!selectedTheme) {
      return;
    }

    try {
      setThemeReleaseStatus(null);
      await bankingOSService.saveThemeDraft({
        ...selectedTheme,
        name: themeEditorName,
        tokens: themeEditorTokens
      });
      await bankingOSService.publishTheme(selectedTheme.code);
      setThemeReleaseStatus({ tone: 'success', message: `${themeEditorName} is now published in BankingOS theme config.` });
      await loadMetadata();
    } catch (err) {
      setThemeReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to publish theme.' });
    }
  };

  const handleBundleAction = async (
    bundleCode: string,
    action: 'submit' | 'approve' | 'reject' | 'promote',
    successMessage: string
  ) => {
    try {
      setBundleReleaseStatus(null);
      const actor = resolveBundleActor();

      if (action === 'submit') {
        await bankingOSService.submitPublishBundle(bundleCode, actor);
      } else if (action === 'approve') {
        await bankingOSService.approvePublishBundle(bundleCode, actor);
      } else if (action === 'reject') {
        await bankingOSService.rejectPublishBundle(bundleCode, actor);
      } else {
        await bankingOSService.promotePublishBundle(bundleCode, actor);
      }

      setBundleReleaseStatus({ tone: 'success', message: successMessage });
      await loadMetadata();
    } catch (err) {
      setBundleReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to update publish bundle.' });
    }
  };

  const createStarterProduct = () => {
    const timestamp = Date.now();
    const starterProduct: BankingOSProductConfiguration = {
      id: `BOS-PROD-${timestamp}`,
      name: 'New BankingOS Product',
      description: 'Starter product configuration created from BankingOS Config Studio.',
      type: 'LOAN',
      currency: 'GHS',
      interestRate: 24,
      interestMethod: 'Flat',
      minAmount: 500,
      maxAmount: 5000,
      minTerm: 1,
      maxTerm: 12,
      defaultTerm: 6,
      status: 'ACTIVE',
      lendingMethodology: 'INDIVIDUAL',
      isGroupLoanEnabled: false,
      supportsJointLiability: false,
      requiresCenter: false,
      requiresGroup: false,
      defaultRepaymentFrequency: 'Monthly',
      allowedRepaymentFrequencies: ['Monthly'],
      supportsWeeklyRepayment: false,
      minimumGroupSize: null,
      maximumGroupSize: null,
      requiresCompulsorySavings: false,
      minimumSavingsToLoanRatio: null,
      requiresGroupApprovalMeeting: false,
      usesMemberLevelUnderwriting: true,
      usesGroupLevelApproval: false,
      loanCyclePolicyType: null,
      maxCycleNumber: null,
      graduatedCycleLimitRulesJson: null,
      attendanceRuleType: null,
      arrearsEligibilityRuleType: null,
      groupGuaranteePolicyType: null,
      meetingCollectionMode: null,
      allowBatchDisbursement: false,
      allowMemberLevelDisbursementAdjustment: false,
      allowTopUpWithinGroup: false,
      allowRescheduleWithinGroup: false,
      groupPenaltyPolicy: null,
      groupDelinquencyPolicy: null,
      groupOfficerAssignmentMode: null,
      groupRules: {
        minMembersRequired: 0,
        maxMembersAllowed: 0,
        minWeeks: null,
        maxWeeks: null,
        requiresCompulsorySavings: false,
        minSavingsToLoanRatio: null,
        requiresGroupApprovalMeeting: false,
        requiresJointLiability: false,
        allowTopUp: false,
        allowReschedule: false,
        maxCycleNumber: null,
        cycleIncrementRulesJson: null,
        defaultRepaymentFrequency: 'Weekly',
        defaultInterestMethod: 'Flat',
        penaltyPolicyJson: null,
        attendanceRuleJson: null,
        eligibilityRuleJson: null,
        meetingCollectionRuleJson: null,
        allocationOrderJson: null,
        accountingProfileJson: null,
        disclosureTemplate: null
      },
      eligibilityRules: {
        requiresKycComplete: true,
        blockOnSevereArrears: true,
        maxAllowedExposure: null,
        minMembershipDays: null,
        minAttendanceRate: null,
        requireCreditBureauCheck: false,
        creditBureauProvider: null,
        minimumCreditScore: null,
        ruleJson: null
      }
    };

    setSelectedProductId(starterProduct.id);
    setProductEditor(starterProduct);
    setProductReleaseStatus(null);
  };

  const updateProductField = (field: keyof BankingOSProductConfiguration, value: string | number | boolean | string[] | null) => {
    setProductEditor((current) => current ? { ...current, [field]: value } : current);
  };

  const updateProductGroupRule = (field: keyof NonNullable<BankingOSProductConfiguration['groupRules']>, value: string | number | boolean | null) => {
    setProductEditor((current) => current ? {
      ...current,
      groupRules: {
        ...(current.groupRules || {
          minMembersRequired: 0,
          maxMembersAllowed: 0,
          requiresCompulsorySavings: false,
          requiresGroupApprovalMeeting: false,
          requiresJointLiability: false,
          allowTopUp: false,
          allowReschedule: false,
          defaultRepaymentFrequency: 'Weekly',
          defaultInterestMethod: 'Flat'
        }),
        [field]: value
      }
    } : current);
  };

  const updateProductEligibilityRule = (field: keyof NonNullable<BankingOSProductConfiguration['eligibilityRules']>, value: string | number | boolean | null) => {
    setProductEditor((current) => current ? {
      ...current,
      eligibilityRules: {
        ...(current.eligibilityRules || {
          requiresKycComplete: true,
          blockOnSevereArrears: true,
          requireCreditBureauCheck: false
        }),
        [field]: value
      }
    } : current);
  };

  const handleSaveProduct = async () => {
    if (!productEditor) {
      return;
    }

    try {
      setProductReleaseStatus(null);
      const exists = products.some((product) => product.id === productEditor.id);
      if (exists) {
        await bankingOSService.updateProduct(productEditor);
        setProductReleaseStatus({ tone: 'success', message: `${productEditor.name} updated in BankingOS Product Factory.` });
      } else {
        await bankingOSService.createProduct(productEditor);
        setProductReleaseStatus({ tone: 'success', message: `${productEditor.name} created in BankingOS Product Factory.` });
      }
      await loadMetadata();
    } catch (err) {
      setProductReleaseStatus({ tone: 'error', message: err instanceof Error ? err.message : 'Failed to save BankingOS product.' });
    }
  };

  return (
    <div className="simple-screen min-h-full space-y-6 p-4 sm:p-6">
      <section className="screen-hero p-6 sm:p-8">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold text-slate-500">BankingOS Workspace</p>
            <h1 className="mt-2 text-3xl font-semibold text-slate-900">Metadata Control Center</h1>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
              This workspace is the bridge from BankInsight into BankingOS. It surfaces the seeded process pack, low-code forms,
              and theme metadata now being served by the live API.
            </p>
          </div>
          <button
            onClick={loadMetadata}
            className="screen-button-secondary inline-flex items-center gap-2"
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh metadata
          </button>
        </div>
      </section>

      {error && (
        <section className="rounded-[24px] border border-danger-200 bg-danger-50 px-5 py-4 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200">
          <div className="flex items-start gap-3">
            <AlertCircle className="mt-0.5 h-5 w-5" />
            <div>
              <div className="font-semibold">Metadata load failed</div>
              <div className="mt-1 text-sm">{error}</div>
            </div>
          </div>
        </section>
      )}

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
      {stats.map((stat) => (
          <div key={stat.label} className="rounded-[24px] border border-white/70 bg-white/85 p-5 shadow-soft dark:border-white/10 dark:bg-slate-900/70">
            <div className="flex items-center justify-between gap-3">
              <div className="text-sm font-medium text-slate-500 dark:text-slate-400">{stat.label}</div>
              {stat.icon}
            </div>
            <div className="mt-3 text-3xl font-heading font-bold text-slate-950 dark:text-white">{stat.value}</div>
          </div>
        ))}
      </section>

      <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
        <div className="mb-4 flex items-center gap-2">
          <Boxes className="h-5 w-5 text-violet-600 dark:text-violet-300" />
          <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Publish bundles</h2>
        </div>
        {bundleReleaseStatus && (
          <div className={`mb-4 rounded-[18px] border px-4 py-3 text-sm ${
            bundleReleaseStatus.tone === 'success'
              ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
              : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
          }`}>
            {bundleReleaseStatus.message}
          </div>
        )}
        <div className="grid gap-4 xl:grid-cols-2">
          {publishBundles.map((bundle) => (
            <div key={bundle.code} className="rounded-[22px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <div className="text-lg font-semibold text-slate-950 dark:text-white">{bundle.name}</div>
                  <div className="mt-1 text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
                    {bundle.code} | {bundle.releaseChannel}
                  </div>
                </div>
                <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                  {bundle.status}
                </span>
              </div>
              <p className="mt-3 text-sm text-slate-600 dark:text-slate-300">{bundle.notes}</p>
              <div className="mt-4 grid grid-cols-3 gap-3">
                <BundleMetric label="Processes" value={bundle.processes.length} />
                <BundleMetric label="Forms" value={bundle.forms.length} />
                <BundleMetric label="Themes" value={bundle.themes.length} />
              </div>
              <div className="mt-4 text-xs font-medium uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                {bundle.requiresApproval ? 'Approval required before release' : 'Can be promoted directly'}
              </div>
              <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                {bundle.lastAction
                  ? `Last action: ${bundle.lastAction} by ${bundle.lastActionBy || 'system'}`
                  : 'No release actions recorded yet.'}
              </div>
              <div className="mt-4 flex flex-wrap gap-3">
                <button
                  onClick={() => handleBundleAction(bundle.code, 'submit', `${bundle.name} submitted for release governance.`)}
                  className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                >
                  Submit
                </button>
                {bundle.requiresApproval && (
                  <>
                    <button
                      onClick={() => handleBundleAction(bundle.code, 'approve', `${bundle.name} approved for promotion.`)}
                      className="rounded-2xl bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-emerald-700"
                    >
                      Approve
                    </button>
                    <button
                      onClick={() => handleBundleAction(bundle.code, 'reject', `${bundle.name} was rejected.`)}
                      className="rounded-2xl bg-rose-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-rose-700"
                    >
                      Reject
                    </button>
                  </>
                )}
                <button
                  onClick={() => handleBundleAction(bundle.code, 'promote', `${bundle.name} promoted successfully.`)}
                  className="rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                >
                  Promote
                </button>
              </div>
            </div>
          ))}
          {!loading && publishBundles.length === 0 && (
            <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400 xl:col-span-2">
              No BankingOS publish bundles are configured yet.
            </div>
          )}
        </div>
      </section>

      <section className="grid grid-cols-1 gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <div className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
          <div className="mb-4 flex items-center gap-2">
            <Workflow className="h-5 w-5 text-brand-600 dark:text-brand-300" />
            <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Seeded process pack</h2>
          </div>
          <div className="space-y-4">
            {(processPack?.processes || []).map((process) => (
              <button
                key={process.code}
                onClick={() => setSelectedProcessCode(process.code)}
                className={`block w-full rounded-[22px] border p-4 text-left transition ${
                  selectedProcess?.code === process.code
                    ? 'border-brand-300 bg-brand-50/70 dark:border-brand-500/40 dark:bg-brand-500/10'
                    : 'border-slate-200 bg-slate-50/80 hover:border-slate-300 hover:bg-white dark:border-slate-700 dark:bg-slate-800/60 dark:hover:bg-slate-800/80'
                }`}
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="text-lg font-semibold text-slate-950 dark:text-white">{process.name}</div>
                    <div className="mt-1 text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
                      {process.code} | {process.module} | {process.entityType}
                    </div>
                  </div>
                  <span className="rounded-full bg-brand-50 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-brand-700 dark:bg-brand-500/10 dark:text-brand-300">
                    {process.triggerType}
                  </span>
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                  {process.stages.map((stage) => (
                    <span key={`${process.code}-${stage.stageCode}`} className="rounded-full bg-white px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-900 dark:text-slate-300">
                      {stage.displayName}
                    </span>
                  ))}
                </div>
              </button>
            ))}
            {!loading && (processPack?.processes.length || 0) === 0 && (
              <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                No BankingOS processes were returned by the API.
              </div>
            )}
          </div>
        </div>

        <div className="space-y-6">
          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Database className="h-5 w-5 text-emerald-600 dark:text-emerald-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Product factory</h2>
            </div>
            <div className="mb-4 flex flex-wrap items-center gap-3">
              {products.map((product) => (
                <button
                  key={product.id}
                  onClick={() => setSelectedProductId(product.id)}
                  className={`rounded-full px-3 py-2 text-xs font-semibold uppercase tracking-[0.16em] transition ${
                    selectedProduct?.id === product.id
                      ? 'bg-slate-950 text-white dark:bg-white dark:text-slate-950'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700'
                  }`}
                >
                  {product.name}
                </button>
              ))}
              <button
                onClick={createStarterProduct}
                className="rounded-full border border-slate-300 bg-white px-3 py-2 text-xs font-semibold uppercase tracking-[0.16em] text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                New starter product
              </button>
            </div>
            {productEditor ? (
              <div className="space-y-4">
                <div className="grid gap-3 md:grid-cols-2">
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Product id
                    <input value={productEditor.id} onChange={(event) => updateProductField('id', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Product name
                    <input value={productEditor.name} onChange={(event) => updateProductField('name', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Product type
                    <input value={productEditor.type} onChange={(event) => updateProductField('type', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Status
                    <input value={productEditor.status} onChange={(event) => updateProductField('status', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Currency
                    <input value={productEditor.currency} onChange={(event) => updateProductField('currency', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Lending methodology
                    <input value={productEditor.lendingMethodology} onChange={(event) => updateProductField('lendingMethodology', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Interest rate
                    <input type="number" value={productEditor.interestRate ?? ''} onChange={(event) => updateProductField('interestRate', event.target.value === '' ? null : Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Default repayment frequency
                    <input value={productEditor.defaultRepaymentFrequency} onChange={(event) => updateProductField('defaultRepaymentFrequency', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Allowed repayment frequencies
                    <input value={productEditor.allowedRepaymentFrequencies.join(', ')} onChange={(event) => updateProductField('allowedRepaymentFrequencies', event.target.value.split(',').map((value) => value.trim()).filter(Boolean))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Minimum amount
                    <input type="number" value={productEditor.minAmount ?? ''} onChange={(event) => updateProductField('minAmount', event.target.value === '' ? null : Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                    Maximum amount
                    <input type="number" value={productEditor.maxAmount ?? ''} onChange={(event) => updateProductField('maxAmount', event.target.value === '' ? null : Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                  </label>
                </div>
                <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                  Description
                  <textarea value={productEditor.description || ''} onChange={(event) => updateProductField('description', event.target.value)} className="mt-2 min-h-[88px] w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                </label>
                <div className="grid gap-4 lg:grid-cols-2">
                  <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                    <div className="text-sm font-semibold text-slate-950 dark:text-white">Group rules</div>
                    <div className="mt-3 grid gap-3 md:grid-cols-2">
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Minimum members
                        <input type="number" value={productEditor.groupRules?.minMembersRequired ?? 0} onChange={(event) => updateProductGroupRule('minMembersRequired', Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Maximum members
                        <input type="number" value={productEditor.groupRules?.maxMembersAllowed ?? 0} onChange={(event) => updateProductGroupRule('maxMembersAllowed', Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Group repayment frequency
                        <input value={productEditor.groupRules?.defaultRepaymentFrequency || 'Weekly'} onChange={(event) => updateProductGroupRule('defaultRepaymentFrequency', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Group interest method
                        <input value={productEditor.groupRules?.defaultInterestMethod || 'Flat'} onChange={(event) => updateProductGroupRule('defaultInterestMethod', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                    </div>
                  </div>
                  <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                    <div className="text-sm font-semibold text-slate-950 dark:text-white">Eligibility rules</div>
                    <div className="mt-3 grid gap-3 md:grid-cols-2">
                      <label className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
                        <input type="checkbox" checked={productEditor.eligibilityRules?.requiresKycComplete ?? true} onChange={(event) => updateProductEligibilityRule('requiresKycComplete', event.target.checked)} />
                        Require KYC complete
                      </label>
                      <label className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
                        <input type="checkbox" checked={productEditor.eligibilityRules?.requireCreditBureauCheck ?? false} onChange={(event) => updateProductEligibilityRule('requireCreditBureauCheck', event.target.checked)} />
                        Require bureau check
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Max allowed exposure
                        <input type="number" value={productEditor.eligibilityRules?.maxAllowedExposure ?? ''} onChange={(event) => updateProductEligibilityRule('maxAllowedExposure', event.target.value === '' ? null : Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Minimum credit score
                        <input type="number" value={productEditor.eligibilityRules?.minimumCreditScore ?? ''} onChange={(event) => updateProductEligibilityRule('minimumCreditScore', event.target.value === '' ? null : Number(event.target.value))} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                    </div>
                  </div>
                </div>
                <div className="flex flex-wrap gap-3">
                  <button
                    onClick={handleSaveProduct}
                    className="rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                  >
                    Save product
                  </button>
                </div>
                {productReleaseStatus && (
                  <div className={`rounded-[16px] border px-4 py-3 text-sm ${
                    productReleaseStatus.tone === 'success'
                      ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                      : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
                  }`}>
                    {productReleaseStatus.message}
                  </div>
                )}
              </div>
            ) : (
              <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                No BankingOS products are available yet. Create a starter product to begin configuring product-driven workflows.
              </div>
            )}
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <FileStack className="h-5 w-5 text-brand-600 dark:text-brand-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Process explorer</h2>
            </div>
            {selectedProcess ? (
              <div className="space-y-4">
                <div className="rounded-[20px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="text-lg font-semibold text-slate-950 dark:text-white">{selectedProcess.name}</div>
                  <div className="mt-1 text-xs uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
                    {selectedProcess.code} | {selectedProcess.triggerType} | {selectedProcess.entityType} | v{selectedProcess.version} | {selectedProcess.status}
                  </div>
                </div>
                {processEditor && (
                  <div className="rounded-[20px] border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900/70">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div>
                        <div className="text-sm font-semibold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">Process release manager</div>
                        <div className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                          Edit process metadata and stages, then save a draft or publish the configured BankingOS process.
                        </div>
                      </div>
                      <div className="flex flex-wrap gap-3">
                        <button
                          onClick={handleSaveProcessDraft}
                          className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                        >
                          Save draft
                        </button>
                        <button
                          onClick={handlePublishProcess}
                          className="rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                        >
                          Publish process
                        </button>
                      </div>
                    </div>
                    <div className="mt-4 grid gap-3 md:grid-cols-2">
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Process name
                        <input value={processEditor.name} onChange={(event) => updateProcessField('name', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Module
                        <input value={processEditor.module} onChange={(event) => updateProcessField('module', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Entity type
                        <input value={processEditor.entityType} onChange={(event) => updateProcessField('entityType', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                      <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                        Trigger type
                        <input value={processEditor.triggerType} onChange={(event) => updateProcessField('triggerType', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                      </label>
                    </div>
                    <div className="mt-4 space-y-3">
                      {processEditor.stages.map((stage, index) => (
                        <div key={`${stage.stageCode}-${index}`} className="rounded-[18px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                          <div className="grid gap-3 md:grid-cols-2">
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Stage name
                              <input value={stage.displayName} onChange={(event) => updateProcessStageField(index, 'displayName', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                            </label>
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Actor role
                              <input value={stage.actorRole} onChange={(event) => updateProcessStageField(index, 'actorRole', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                            </label>
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Form code
                              <input value={stage.formCode || ''} onChange={(event) => updateProcessStageField(index, 'formCode', event.target.value)} className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80" />
                            </label>
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Actions
                              <input
                                value={stage.actions.join(', ')}
                                onChange={(event) => updateProcessStageField(index, 'actions', event.target.value.split(',').map((value) => value.trim()).filter(Boolean))}
                                className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                              />
                            </label>
                          </div>
                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Screen title
                              <input
                                value={stage.screen?.title || stage.displayName}
                                onChange={(event) => updateProcessStageScreenField(index, 'title', event.target.value)}
                                className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                              />
                            </label>
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Screen description
                              <input
                                value={stage.screen?.description || ''}
                                onChange={(event) => updateProcessStageScreenField(index, 'description', event.target.value)}
                                className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                              />
                            </label>
                            <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                              Banner tone
                              <select
                                value={stage.screen?.bannerTone || 'neutral'}
                                onChange={(event) => updateProcessStageScreenField(index, 'bannerTone', event.target.value)}
                                className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                              >
                                <option value="neutral">Neutral</option>
                                <option value="info">Info</option>
                                <option value="success">Success</option>
                                <option value="warning">Warning</option>
                                <option value="danger">Danger</option>
                              </select>
                            </label>
                          </div>
                          <label className="mt-3 block text-sm font-medium text-slate-700 dark:text-slate-200">
                            Guidance banner
                            <textarea
                              value={stage.screen?.bannerMessage || ''}
                              onChange={(event) => updateProcessStageScreenField(index, 'bannerMessage', event.target.value)}
                              className="mt-2 min-h-[88px] w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                              placeholder="Stage-specific operator guidance shown in the runtime workbench."
                            />
                          </label>
                          <div className="mt-4 rounded-[16px] border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
                            <div className="flex flex-wrap items-center justify-between gap-3">
                              <div>
                                <div className="text-sm font-semibold text-slate-950 dark:text-white">Screen sections</div>
                                <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                                  Define the layout blocks the BankingOS runtime should render for this stage.
                                </div>
                              </div>
                              <button
                                type="button"
                                onClick={() => addProcessStageScreenSection(index)}
                                className="rounded-2xl border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                              >
                                Add section
                              </button>
                            </div>
                            <div className="mt-4 space-y-3">
                              {(stage.screen?.sections || []).map((section, sectionIndex) => (
                                <div key={`${section.id || 'section'}-${sectionIndex}`} className="rounded-[16px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                                  <div className="grid gap-3 md:grid-cols-2">
                                    <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                                      Section id
                                      <input
                                        value={section.id}
                                        onChange={(event) => updateProcessStageScreenSection(index, sectionIndex, 'id', event.target.value)}
                                        className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                                      />
                                    </label>
                                    <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                                      Section title
                                      <input
                                        value={section.title}
                                        onChange={(event) => updateProcessStageScreenSection(index, sectionIndex, 'title', event.target.value)}
                                        className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                                      />
                                    </label>
                                    <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                                      Section kind
                                      <select
                                        value={section.kind}
                                        onChange={(event) => updateProcessStageScreenSection(index, sectionIndex, 'kind', event.target.value)}
                                        className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                                      >
                                        <option value="summary">Summary</option>
                                        <option value="form">Form</option>
                                        <option value="guidance">Guidance</option>
                                      </select>
                                    </label>
                                    <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
                                      Field ids
                                      <input
                                        value={section.fieldIds.join(', ')}
                                        onChange={(event) => updateProcessStageScreenSection(index, sectionIndex, 'fieldIds', event.target.value.split(',').map((value) => value.trim()).filter(Boolean))}
                                        className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                                        placeholder="customer_id, product_code"
                                      />
                                    </label>
                                  </div>
                                  <div className="mt-3 flex justify-end">
                                    <button
                                      type="button"
                                      onClick={() => removeProcessStageScreenSection(index, sectionIndex)}
                                      className="rounded-2xl bg-rose-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-700"
                                    >
                                      Remove section
                                    </button>
                                  </div>
                                </div>
                              ))}
                              {(stage.screen?.sections || []).length === 0 && (
                                <div className="rounded-[14px] border border-dashed border-slate-300 px-4 py-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                                  No screen sections defined yet. Add one to control the runtime layout for this stage.
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                    {processReleaseStatus && (
                      <div className={`mt-4 rounded-[16px] border px-4 py-3 text-sm ${
                        processReleaseStatus.tone === 'success'
                          ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                          : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
                      }`}>
                        {processReleaseStatus.message}
                      </div>
                    )}
                  </div>
                )}
                <div className="space-y-3">
                  {selectedProcess.stages.map((stage, index) => {
                    const linkedForm = forms.find((form) => form.code === stage.formCode);
                    return (
                      <div key={`${selectedProcess.code}-${stage.stageCode}`} className="rounded-[20px] border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900/70">
                        <div className="flex items-start gap-4">
                          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-50 text-sm font-bold text-brand-700 dark:bg-brand-500/10 dark:text-brand-300">
                            {index + 1}
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="flex flex-wrap items-center justify-between gap-3">
                              <div className="font-semibold text-slate-950 dark:text-white">{stage.displayName}</div>
                              <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                                {stage.actorRole}
                              </span>
                            </div>
                            <div className="mt-1 text-xs text-slate-500 dark:text-slate-400">{stage.stageCode}</div>
                            {stage.screen?.title && (
                              <div className="mt-2 text-sm text-slate-600 dark:text-slate-300">
                                Runtime screen: {stage.screen.title}
                              </div>
                            )}
                            {linkedForm && (
                              <div className="mt-3 rounded-[16px] border border-slate-200 bg-slate-50 px-3 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                                <div className="text-xs font-semibold uppercase tracking-[0.16em] text-brand-700 dark:text-brand-300">
                                  Linked form
                                </div>
                                <div className="mt-1 text-sm font-medium text-slate-900 dark:text-white">{linkedForm.name}</div>
                                <div className="mt-2 flex flex-wrap gap-2">
                                  {linkedForm.fields.slice(0, 5).map((field) => (
                                    <span key={field.id} className="rounded-full bg-white px-2.5 py-1 text-[11px] font-semibold text-slate-600 dark:bg-slate-900 dark:text-slate-300">
                                      {field.label}
                                    </span>
                                  ))}
                                  {linkedForm.fields.length > 5 && (
                                    <span className="rounded-full bg-white px-2.5 py-1 text-[11px] font-semibold text-slate-600 dark:bg-slate-900 dark:text-slate-300">
                                      +{linkedForm.fields.length - 5} more
                                    </span>
                                  )}
                                </div>
                              </div>
                            )}
                            <div className="mt-3 flex flex-wrap gap-2">
                              {stage.actions.map((action) => (
                                <span key={action} className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                                  {action.replaceAll('_', ' ')}
                                </span>
                              ))}
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ) : (
              <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                Select a process to inspect its runtime stages and linked forms.
              </div>
            )}
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Database className="h-5 w-5 text-blue-600 dark:text-blue-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Linked form previews</h2>
            </div>
            <div className="space-y-3">
              {linkedForms.map((form) => (
                <div key={form.code} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="font-semibold text-slate-950 dark:text-white">{form.name}</div>
                  <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                    {form.code} | {form.module} | v{form.version}
                  </div>
                  <div className="mt-3 grid gap-2">
                    {form.fields.slice(0, 4).map((field) => (
                      <div key={field.id} className="rounded-[14px] border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900">
                        <div className="font-medium text-slate-900 dark:text-white">{field.label}</div>
                        <div className="mt-1 text-xs uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
                          {field.type} {field.required ? '| required' : '| optional'}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
              {!loading && linkedForms.length === 0 && (
                <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                  The selected process does not currently reference a seeded form.
                </div>
              )}
              {primaryLinkedForm && (
                <div className="rounded-[20px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <div className="text-sm font-semibold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">Form release manager</div>
                      <div className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                        Save the linked form into BankingOS config storage, then promote it to published status.
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-3">
                      <button
                        onClick={handleSaveLinkedFormDraft}
                        className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                      >
                        Save draft
                      </button>
                      <button
                        onClick={handlePublishLinkedForm}
                        className="rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                      >
                        Publish form
                      </button>
                    </div>
                  </div>
                  {formReleaseStatus && (
                    <div className={`mt-4 rounded-[16px] border px-4 py-3 text-sm ${
                      formReleaseStatus.tone === 'success'
                        ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                        : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
                    }`}>
                      {formReleaseStatus.message}
                    </div>
                  )}
                </div>
              )}
            </div>
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Workflow className="h-5 w-5 text-brand-600 dark:text-brand-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Versioned process catalog</h2>
            </div>
            <div className="space-y-3">
              {processCatalog.map((process) => (
                <div key={process.code} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-950 dark:text-white">{process.name}</div>
                      <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                        {process.code} | {process.module} | v{process.version}
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                        {process.status}
                      </span>
                      <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] ${
                        process.isSeeded
                          ? 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300'
                          : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300'
                      }`}>
                        {process.isSeeded ? 'Seeded' : 'Configured'}
                      </span>
                    </div>
                  </div>
                  <div className="mt-3 text-sm text-slate-600 dark:text-slate-300">
                    {process.stageCount} stages {process.isPublished ? '| published release' : '| not yet published'}
                  </div>
                </div>
              ))}
              {!loading && processCatalog.length === 0 && (
                <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                  No BankingOS processes are registered in the catalog yet.
                </div>
              )}
            </div>
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <FileStack className="h-5 w-5 text-blue-600 dark:text-blue-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Versioned form catalog</h2>
            </div>
            <div className="space-y-3">
              {formCatalog.map((form) => (
                <div key={form.code} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-950 dark:text-white">{form.name}</div>
                      <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                        {form.code} | {form.module} | v{form.version}
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                        {form.status}
                      </span>
                      <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] ${
                        form.isSeeded
                          ? 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300'
                          : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300'
                      }`}>
                        {form.isSeeded ? 'Seeded' : 'Configured'}
                      </span>
                    </div>
                  </div>
                  <div className="mt-3 text-sm text-slate-600 dark:text-slate-300">
                    {form.fieldCount} fields {form.isPublished ? '| published release' : '| not yet published'}
                  </div>
                </div>
              ))}
              {!loading && formCatalog.length === 0 && (
                <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                  No BankingOS forms are registered in the catalog yet.
                </div>
              )}
            </div>
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Send className="h-5 w-5 text-emerald-600 dark:text-emerald-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Process launchpad</h2>
            </div>
            {primaryLinkedForm && selectedProcess ? (
              <div className="space-y-4">
                <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="font-semibold text-slate-950 dark:text-white">{selectedProcess.name}</div>
                  <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">
                    Launches the BankingOS runtime with server-owned product options, field rules, and launch validation.
                  </div>
                </div>
                {launchContext && (
                  <div className="grid gap-4 lg:grid-cols-2">
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-sm font-semibold text-slate-950 dark:text-white">Launch validation hints</div>
                      <div className="mt-3 space-y-2 text-sm text-slate-600 dark:text-slate-300">
                        {launchContext.validationHints.length > 0 ? launchContext.validationHints.map((hint) => (
                          <div key={hint}>{hint}</div>
                        )) : <div>No additional launch hints are configured for this process.</div>}
                      </div>
                    </div>
                    <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                      <div className="text-sm font-semibold text-slate-950 dark:text-white">Eligible products</div>
                      <div className="mt-3 space-y-3">
                        {launchContext.productOptions.map((product) => (
                          <div key={product.productId} className="rounded-[16px] border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                            <div className="flex flex-wrap items-start justify-between gap-2">
                              <div>
                                <div className="font-medium text-slate-950 dark:text-white">{product.name}</div>
                                <div className="mt-1 text-xs uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
                                  {product.productId} | {product.type} | {product.status}
                                </div>
                              </div>
                              <div className="text-right text-xs text-slate-500 dark:text-slate-400">
                                {product.minAmount ?? 0} - {product.maxAmount ?? 'No max'} {product.currency}
                              </div>
                            </div>
                            {product.eligibilityHints.length > 0 && (
                              <div className="mt-2 flex flex-wrap gap-2">
                                {product.eligibilityHints.map((hint) => (
                                  <span key={hint} className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                                    {hint}
                                  </span>
                                ))}
                              </div>
                            )}
                          </div>
                        ))}
                        {launchContext.productOptions.length === 0 && (
                          <div className="text-sm text-slate-500 dark:text-slate-400">
                            No configured products currently match this BankingOS process.
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                )}
                <div className="grid gap-3">
                  {primaryLinkedForm.fields.map((field) => (
                    <label key={field.id} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-200">
                      <div className="flex items-center justify-between gap-3">
                        <span>{field.label}</span>
                        <span className="text-xs uppercase tracking-[0.14em] text-slate-400 dark:text-slate-500">
                          {field.type}{field.required ? ' | required' : ''}
                        </span>
                      </div>
                      {field.options && field.options.length > 0 ? (
                        <select
                          value={runnerValues[field.id] || ''}
                          onChange={(event) => updateRunnerValue(field.id, event.target.value)}
                          className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                        >
                          <option value="">Select option</option>
                          {field.options.map((option) => (
                            <option key={option} value={option}>{option}</option>
                          ))}
                        </select>
                      ) : (
                        <input
                          type={field.type === 'number' || field.type === 'currency' ? 'number' : 'text'}
                          value={runnerValues[field.id] || ''}
                          onChange={(event) => updateRunnerValue(field.id, event.target.value)}
                          className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                          placeholder={field.label}
                        />
                      )}
                    </label>
                  ))}
                </div>
                {launchResult && (
                  <div className={`rounded-[18px] border px-4 py-3 text-sm ${
                    launchResult.tone === 'success'
                      ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                      : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
                  }`}>
                    {launchResult.message}
                  </div>
                )}
                <button
                  onClick={handleLaunchProcess}
                  disabled={launching}
                  className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-950 px-4 py-3 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                >
                  <Send className="h-4 w-4" />
                  {launching ? 'Launching process...' : `Launch ${selectedProcess.name}`}
                </button>
              </div>
            ) : (
              <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                Select a process with a linked form to launch a seeded BankingOS instance.
              </div>
            )}
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Boxes className="h-5 w-5 text-violet-600 dark:text-violet-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Active BankingOS tasks</h2>
            </div>
            {taskActionStatus && (
              <div className={`mb-4 rounded-[18px] border px-4 py-3 text-sm ${
                taskActionStatus.tone === 'success'
                  ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                  : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
              }`}>
                {taskActionStatus.message}
              </div>
            )}
            <div className="space-y-6">
              <div>
                <div className="mb-3 text-sm font-semibold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                  My tasks
                </div>
                <div className="space-y-3">
                  {processTasks.mine.map((task) => (
                    <div key={task.id} className={`rounded-[18px] border px-4 py-4 dark:border-slate-700 ${
                      selectedRuntimeTaskId === task.id
                        ? 'border-brand-300 bg-brand-50/70 dark:bg-brand-500/10'
                        : 'border-slate-200 bg-slate-50/80 dark:bg-slate-800/60'
                    }`}>
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <div className="font-semibold text-slate-950 dark:text-white">{task.processStepDefinition?.stepName || 'Task'}</div>
                          <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            {task.processStepDefinition?.stepType || 'Task'} | {task.processInstance?.entityType} #{task.processInstance?.entityId}
                          </div>
                        </div>
                        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                          {task.status}
                        </span>
                      </div>
                      <div className="mt-4 flex flex-wrap gap-3">
                        <button
                          onClick={() => setSelectedRuntimeTaskId(task.id)}
                          className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                        >
                          Open workbench
                        </button>
                        <button
                          onClick={() => handleCompleteTask(task)}
                          className="rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                        >
                          {task.processStepDefinition?.stepType === 'ApprovalTask' ? 'Approve task' : 'Complete task'}
                        </button>
                        {task.processStepDefinition?.stepType === 'ApprovalTask' && (
                          <button
                            onClick={() => handleRejectTask(task.id)}
                            className="rounded-2xl bg-rose-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-rose-700"
                          >
                            Reject task
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                  {!loading && processTasks.mine.length === 0 && (
                    <div className="rounded-[18px] border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                      No assigned BankingOS tasks for this process yet.
                    </div>
                  )}
                </div>
              </div>

              <div>
                <div className="mb-3 text-sm font-semibold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                  Claimable queue
                </div>
                <div className="space-y-3">
                  {processTasks.claimable.map((task) => (
                    <div key={task.id} className={`rounded-[18px] border px-4 py-4 dark:border-slate-700 ${
                      selectedRuntimeTaskId === task.id
                        ? 'border-brand-300 bg-brand-50/70 dark:bg-brand-500/10'
                        : 'border-slate-200 bg-slate-50/80 dark:bg-slate-800/60'
                    }`}>
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <div className="font-semibold text-slate-950 dark:text-white">{task.processStepDefinition?.stepName || 'Task'}</div>
                          <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                            Queue: {task.assignedRoleCode || task.assignedPermissionCode || 'Unassigned'}
                          </div>
                        </div>
                        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                          {task.status}
                        </span>
                      </div>
                      <div className="mt-4 flex flex-wrap gap-3">
                        <button
                          onClick={() => setSelectedRuntimeTaskId(task.id)}
                          className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                        >
                          Open workbench
                        </button>
                        <button
                          onClick={() => handleClaimTask(task.id)}
                          className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                        >
                          Claim task
                        </button>
                      </div>
                    </div>
                  ))}
                  {!loading && processTasks.claimable.length === 0 && (
                    <div className="rounded-[18px] border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                      No claimable BankingOS tasks for this process right now.
                    </div>
                  )}
                </div>
              </div>
            </div>
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Send className="h-5 w-5 text-brand-600 dark:text-brand-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Task workbench</h2>
            </div>
            {selectedRuntimeTask ? (
              <div className="space-y-4">
                <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-950 dark:text-white">
                        {taskContext?.screen?.title || taskContext?.stepName || selectedRuntimeTask.processStepDefinition?.stepName || 'Task'}
                      </div>
                      <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                        {(taskContext?.stepCode || selectedRuntimeTask.processStepDefinition?.stepCode)} | {(taskContext?.stepType || selectedRuntimeTask.processStepDefinition?.stepType)} | {selectedRuntimeTask.status}
                      </div>
                      {taskContext?.screen?.description && (
                        <div className="mt-2 text-sm text-slate-600 dark:text-slate-300">
                          {taskContext.screen.description}
                        </div>
                      )}
                    </div>
                    <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                      {(taskContext?.entityType || selectedRuntimeTask.processInstance?.entityType)} #{(taskContext?.entityId || selectedRuntimeTask.processInstance?.entityId)}
                    </span>
                  </div>
                  {selectedRuntimeStage && (
                    <div className="mt-3 text-sm text-slate-600 dark:text-slate-300">
                      Stage: {selectedRuntimeStage.displayName} | Role: {selectedRuntimeStage.actorRole}
                    </div>
                  )}
                </div>

                {taskContext?.screen?.bannerMessage && (
                  <div className={`rounded-[18px] border px-4 py-3 text-sm ${
                    taskContext.screen.bannerTone === 'info'
                      ? 'border-sky-200 bg-sky-50 text-sky-800 dark:border-sky-500/30 dark:bg-sky-500/10 dark:text-sky-200'
                      : taskContext.screen.bannerTone === 'success'
                        ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                        : taskContext.screen.bannerTone === 'warning'
                          ? 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-200'
                          : taskContext.screen.bannerTone === 'danger'
                            ? 'border-rose-200 bg-rose-50 text-rose-800 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-200'
                      : 'border-slate-200 bg-slate-50 text-slate-700 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-200'
                  }`}>
                    {taskContext.screen.bannerMessage}
                  </div>
                )}

                {taskContext?.selectedProduct && (
                  <div className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <div className="text-sm font-semibold text-slate-950 dark:text-white">Selected product</div>
                        <div className="mt-1 text-base font-medium text-slate-900 dark:text-white">{taskContext.selectedProduct.name}</div>
                        <div className="mt-1 text-xs uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
                          {taskContext.selectedProduct.productId} | {taskContext.selectedProduct.type} | {taskContext.selectedProduct.status}
                        </div>
                      </div>
                      <div className="text-right text-sm text-slate-600 dark:text-slate-300">
                        {taskContext.selectedProduct.minAmount ?? 0} - {taskContext.selectedProduct.maxAmount ?? 'No max'} {taskContext.selectedProduct.currency}
                      </div>
                    </div>
                    {taskContext.selectedProduct.eligibilityHints.length > 0 && (
                      <div className="mt-3 flex flex-wrap gap-2">
                        {taskContext.selectedProduct.eligibilityHints.map((hint) => (
                          <span key={hint} className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                            {hint}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                )}

                {screenSections.map((section) => {
                  if (section.kind === 'summary') {
                    return (
                      <div key={section.id} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                        <div className="text-sm font-semibold text-slate-950 dark:text-white">{section.title}</div>
                        <div className="mt-2 grid gap-2 text-sm text-slate-600 dark:text-slate-300 md:grid-cols-2">
                          <div>Process: {taskContext?.processName || selectedProcess?.name || 'Unknown process'}</div>
                          <div>Outcome: {taskContext?.completionOutcome || 'Complete'}</div>
                          <div>Entity: {(taskContext?.entityType || selectedRuntimeTask.processInstance?.entityType)} #{(taskContext?.entityId || selectedRuntimeTask.processInstance?.entityId)}</div>
                          <div>Assigned role: {selectedRuntimeStage?.actorRole || 'Runtime resolved'}</div>
                        </div>
                      </div>
                    );
                  }

                  if (section.kind === 'guidance') {
                    return (
                      <div key={section.id} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                        <div className="text-sm font-semibold text-slate-950 dark:text-white">{section.title}</div>
                        <div className="mt-3 grid gap-3 md:grid-cols-2">
                          <div className="rounded-[16px] border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                            <div className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Available actions</div>
                            <div className="mt-2 flex flex-wrap gap-2">
                              {taskActions.map((action) => (
                                <span key={action.code} className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                                  {action.label}
                                </span>
                              ))}
                            </div>
                          </div>
                          <div className="rounded-[16px] border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
                            <div className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Validation policy</div>
                            <div className="mt-2 space-y-2 text-sm text-slate-600 dark:text-slate-300">
                              {validationRules.length > 0 ? validationRules.map((rule) => (
                                <div key={rule.fieldId}>{rule.message}</div>
                              )) : <div>No required field rules are configured for this stage.</div>}
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  }

                  const fields = section.fieldIds
                    .map((fieldId) => runtimeFieldMap.get(fieldId))
                    .filter((field): field is BankingOSSeedField => Boolean(field));

                  return (
                    <div key={section.id} className="space-y-3">
                      <div className="text-sm font-semibold text-slate-950 dark:text-white">{section.title}</div>
                      {fields.length > 0 ? (
                        <div className="grid gap-3">
                          {fields.map((field) => (
                            <label key={field.id} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-3 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-200">
                              <div className="flex items-center justify-between gap-3">
                                <span>{field.label}</span>
                                <span className="text-xs uppercase tracking-[0.14em] text-slate-400 dark:text-slate-500">
                                  {field.type}{field.required ? ' | required' : ''}
                                </span>
                              </div>
                              {field.options && field.options.length > 0 ? (
                                <select
                                  value={taskRunnerValues[field.id] || ''}
                                  onChange={(event) => updateTaskRunnerValue(field.id, event.target.value)}
                                  className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                                >
                                  <option value="">Select option</option>
                                  {field.options.map((option) => (
                                    <option key={option} value={option}>{option}</option>
                                  ))}
                                </select>
                              ) : (
                                <input
                                  type={field.type === 'number' || field.type === 'currency' ? 'number' : 'text'}
                                  value={taskRunnerValues[field.id] || ''}
                                  onChange={(event) => updateTaskRunnerValue(field.id, event.target.value)}
                                  className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                                  placeholder={field.label}
                                />
                              )}
                            </label>
                          ))}
                        </div>
                      ) : (
                        <div className="rounded-[18px] border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                          No mapped fields are available for this section yet.
                        </div>
                      )}
                    </div>
                  );
                })}

                {!selectedRuntimeForm && screenSections.length === 0 && (
                  <div className="rounded-[18px] border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                    No linked BankingOS form is configured for this stage yet. You can still act on the task directly.
                  </div>
                )}

                <label className="block text-sm font-medium text-slate-700 dark:text-slate-200">
                  Remarks
                  <textarea
                    value={taskRemarks}
                    onChange={(event) => setTaskRemarks(event.target.value)}
                    className="mt-2 min-h-[96px] w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                    placeholder="Add operational notes, review comments, or rejection rationale."
                  />
                </label>

                <div className="flex flex-wrap gap-3">
                  {taskActions.map((action) => (
                    <button
                      key={action.code}
                      onClick={() => handleWorkbenchAction(action.code)}
                      disabled={!action.isEnabled}
                      title={action.disabledReason || action.label}
                      className={`rounded-2xl px-4 py-2.5 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-60 ${
                        action.tone === 'danger'
                          ? 'bg-rose-600 text-white hover:bg-rose-700'
                          : action.tone === 'primary'
                            ? 'bg-slate-950 text-white hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200'
                            : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800'
                      }`}
                    >
                      {action.label}
                    </button>
                  ))}
                </div>
              </div>
            ) : (
              <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                Open a BankingOS task to render its stage-aware workbench from configured process and form metadata.
              </div>
            )}
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Palette className="h-5 w-5 text-amber-600 dark:text-amber-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Theme presets</h2>
            </div>
            <div className="mb-4 flex flex-wrap gap-2">
              {themes.map((theme) => (
                <button
                  key={theme.code}
                  onClick={() => setSelectedThemeCode(theme.code)}
                  className={`rounded-full px-3 py-2 text-xs font-semibold uppercase tracking-[0.16em] transition ${
                    selectedTheme?.code === theme.code
                      ? 'bg-slate-950 text-white dark:bg-white dark:text-slate-950'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700'
                  }`}
                >
                  {theme.name}
                </button>
              ))}
            </div>
            <div className="space-y-3">
              {selectedTheme && (
                <div key={selectedTheme.code} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-950 dark:text-white">{selectedTheme.name}</div>
                      <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                        {selectedTheme.code} | v{selectedTheme.version} | {selectedTheme.status}
                      </div>
                    </div>
                    <div
                      className="h-10 w-10 rounded-2xl border border-slate-200"
                      style={{ backgroundColor: themeEditorTokens['color.primary'] || selectedTheme.tokens['color.primary'] || '#0F4C81' }}
                    />
                  </div>
                  <div className="mt-4 rounded-[18px] border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
                    <div className="mb-4 flex items-center justify-between gap-3">
                      <div>
                        <div className="text-sm font-semibold text-slate-950 dark:text-white">Theme release manager</div>
                        <div className="text-xs text-slate-500 dark:text-slate-400">Edit tokens, save a draft version, then publish to BankingOS config.</div>
                      </div>
                      <div className="flex flex-wrap gap-3">
                        <button
                          onClick={handleSaveThemeDraft}
                          className="rounded-2xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                        >
                          Save draft
                        </button>
                        <button
                          onClick={handlePublishTheme}
                          className="rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 dark:bg-white dark:text-slate-950 dark:hover:bg-slate-200"
                        >
                          Publish theme
                        </button>
                      </div>
                    </div>
                    <label className="mb-4 block text-sm font-medium text-slate-700 dark:text-slate-200">
                      Theme name
                      <input
                        value={themeEditorName}
                        onChange={(event) => setThemeEditorName(event.target.value)}
                        className="mt-2 w-full rounded-2xl border border-slate-300/90 bg-white px-3.5 py-3 text-sm dark:border-slate-600 dark:bg-slate-950/80"
                      />
                    </label>
                    <div className="grid gap-2">
                      {Object.entries(themeEditorTokens).map(([token, value]) => (
                        <div key={token} className="flex items-center justify-between rounded-[14px] border border-slate-200 px-3 py-2 text-sm dark:border-slate-700">
                          <span className="font-medium text-slate-700 dark:text-slate-300">{token}</span>
                          <input
                            value={value}
                            onChange={(event) => updateThemeToken(token, event.target.value)}
                            className="w-48 rounded-xl border border-slate-300/90 bg-white px-3 py-2 text-right text-sm text-slate-500 dark:border-slate-600 dark:bg-slate-950/80 dark:text-slate-300"
                          />
                        </div>
                      ))}
                    </div>
                    {themeReleaseStatus && (
                      <div className={`mt-4 rounded-[16px] border px-4 py-3 text-sm ${
                        themeReleaseStatus.tone === 'success'
                          ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200'
                          : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-200'
                      }`}>
                        {themeReleaseStatus.message}
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </section>

          <section className="rounded-[28px] border border-white/70 bg-white/90 p-6 shadow-soft dark:border-white/10 dark:bg-slate-900/78">
            <div className="mb-4 flex items-center gap-2">
              <Brush className="h-5 w-5 text-amber-600 dark:text-amber-300" />
              <h2 className="text-xl font-heading font-bold text-slate-950 dark:text-white">Versioned theme catalog</h2>
            </div>
            <div className="space-y-3">
              {themeCatalog.map((theme) => (
                <div key={theme.code} className="rounded-[18px] border border-slate-200 bg-slate-50/80 px-4 py-4 dark:border-slate-700 dark:bg-slate-800/60">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-950 dark:text-white">{theme.name}</div>
                      <div className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
                        {theme.code} | v{theme.version}
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                        {theme.status}
                      </span>
                      <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] ${
                        theme.isSeeded
                          ? 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300'
                          : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300'
                      }`}>
                        {theme.isSeeded ? 'Seeded' : 'Configured'}
                      </span>
                    </div>
                  </div>
                  <div className="mt-3 text-sm text-slate-600 dark:text-slate-300">
                    {theme.tokenCount} tokens {theme.isPublished ? '| published release' : '| not yet published'}
                  </div>
                </div>
              ))}
              {!loading && themeCatalog.length === 0 && (
                <div className="rounded-[20px] border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
                  No BankingOS themes are registered in the catalog yet.
                </div>
              )}
            </div>
          </section>
        </div>
      </section>
    </div>
  );
}

function BundleMetric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-[16px] border border-slate-200 bg-white px-3 py-3 text-center dark:border-slate-700 dark:bg-slate-900">
      <div className="text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">{label}</div>
      <div className="mt-1 text-lg font-heading font-bold text-slate-950 dark:text-white">{value}</div>
    </div>
  );
}
