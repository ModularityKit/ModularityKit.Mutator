# ADR Index

This document lists all architectural decisions (ADRs) for **ModularityKit.Mutators**.  
See each ADR for full rationale, context, and decision details.

## Core

These ADRs describe the base `ModularityKit.Mutator` runtime and its execution model.

| ADR     | Title                                                | Link                                                                                         |
| ------- | ---------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| ADR-001 | StateChange and ChangeSet Model                      | [ADR-001](Adr/ADR_001_StateChange_ChangeSet_Model.md)                          |
| ADR-002 | Mutation Context and Actor Type                      | [ADR-002](Adr/ADR_002_Mutation_Context_and_ActorType.md)                       |
| ADR-003 | MutationIntent and BlastRadius                       | [ADR-003](Adr/ADR_003_MutationIntent_and_BlastRadius.md)                       |
| ADR-004 | Mutation Policies and PolicyDecision                 | [ADR-004](Adr/ADR_004_Mutation_Policies_and_PolicyDecision.md)                 |
| ADR-005 | Mutation Audit Abstractions                          | [ADR-005](Adr/ADR_005_Mutation_Audit_Abstractions.md)                          |
| ADR-006 | Mutation Side Effects                                | [ADR-006](Adr/ADR_006_Mutation_Side_Effects.md)                                |
| ADR-007 | Mutation History and Audit                           | [ADR-007](Adr/ADR_007_Mutation_History_and_Audit.md)                           |
| ADR-008 | Mutation Interceptor                                 | [ADR-008](Adr/ADR_008_Mutation_Interceptor.md)                                 |
| ADR-009 | Mutation Metrics                                     | [ADR-009](Adr/ADR_009_Mutation_Metrics.md)                                     |
| ADR-010 | Mutation Result Model                                | [ADR-010](Adr/ADR_010_Mutation_Result_Model.md)                                |
| ADR-011 | Execution Context for Mutation Runtime               | [ADR-011](Adr/ADR_011_Execution_Context_for_Mutation_Runtime.md)               |
| ADR-012 | Mutation Execution Interfaces and Context Separation | [ADR-012](Adr/ADR_012_Mutation_Execution_Interfaces_and_Context_Separation.md) |
| ADR-013 | Mutation Engine and Executor Runtime Integration     | [ADR-013](Adr/ADR_013_Mutation_Engine_and_Executor_Runtime_Integration.md)     |
| ADR-014 | InMemory Auditor and HistoryStore                    | [ADR-014](Adr/ADR_014_InMemory_Auditor_and_HistoryStore.md)                    |
| ADR-015 | Mutation Interceptor Pipeline                        | [ADR-015](Adr/ADR_015_Mutation_Interceptor_Pipeline.md)                        |
| ADR-016 | Mutation Metrics Collection                          | [ADR-016](Adr/ADR_016_Mutation_Metrics_Collection.md)                          |
| ADR-017 | Mutation PolicyRegistry                              | [ADR-017](Adr/ADR_017_Mutation_PolicyRegistry.md)                              |
| ADR-018 | Mutators DI Registration                             | [ADR-018](Adr/ADR_018_Mutators_DI_Registration.md)                             |

## Governance

These ADRs describe the `ModularityKit.Mutator.Governance` extension layer and its request-based governance model.

| ADR     | Title                                                | Link                                                                                         |
| ------- | ---------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| ADR-019 | Governance Package Separation                        | [ADR-019](Adr/ADR_019_Governance_Package_Separation.md)                        |
| ADR-020 | Governance MutationRequest Model                     | [ADR-020](Adr/ADR_020_Governance_MutationRequest_Model.md)                     |
| ADR-021 | Governance Pending Mutation Lifecycle                | [ADR-021](Adr/ADR_021_Governance_Pending_Mutation_Lifecycle.md)                |
| ADR-022 | Governance Request Decisions and Storage             | [ADR-022](Adr/ADR_022_Governance_Request_Decisions_and_Storage.md)             |
| ADR-023 | Governance Versioned Request Resolution              | [ADR-023](Adr/ADR_023_Governance_Versioned_Request_Resolution.md)              |
| ADR-024 | Governance Runtime Pending Request Handling          | [ADR-024](Adr/ADR_024_Governance_Runtime_Pending_Request_Handling.md)          |
| ADR-025 | Governance Approval Workflow                         | [ADR-025](Adr/ADR_025_Governance_Approval_Workflow.md)                         |
| ADR-026 | Governance Request Query API                         | [ADR-026](Adr/ADR_026_Governance_Request_Query_API.md)                         |
| ADR-027 | Governed Execution Manager                           | [ADR-027](Adr/ADR_027_Governed_Execution_Manager.md)                           |
| ADR-028 | Governance Approval Workflow Hardening               | [ADR-028](Adr/ADR_028_Governance_Approval_Workflow_Hardening.md)               |
| ADR-029 | Governance Redis Provider Package                    | [ADR-029](Adr/ADR_029_Governance_Redis_Provider_Package.md)                    |
| ADR-030 | Governance Redis Request Storage and Query Strategy  | [ADR-030](Adr/ADR_030_Governance_Redis_Request_Storage_and_Query_Strategy.md)  |
| ADR-031 | Governance Redis Serialization and Document Compatibility | [ADR-031](Adr/ADR_031_Governance_Redis_Serialization_and_Document_Compatibility.md) |
| ADR-032 | Governance Redis Concurrency and Index Maintenance Model | [ADR-032](Adr/ADR_032_Governance_Redis_Concurrency_and_Index_Maintenance_Model.md) |
| ADR-033 | Governance Query Model Decomposition                | [ADR-033](Adr/ADR_033_Governance_Query_Model_Decomposition.md)                 |
| ADR-034 | Governed Execution Compensation Model               | [ADR-034](Adr/ADR_034_Governed_Execution_Compensation_Model.md)                |

> See individual ADRs for detailed context, decision rationale, and consequences.
