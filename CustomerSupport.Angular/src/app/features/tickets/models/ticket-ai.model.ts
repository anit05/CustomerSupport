/** Matches the final provider-independent CustomerSupport.API contracts. */
export interface TicketAIAnalysis {
  problem: string;
  actionsTried: string[];
  nextStep: string;
  sentiment: string;
  suggestedCategory: string | null;
  suggestedPriority: string | null;
  confirmedResolutionDate: string | null;
  requiresHumanReview: boolean;
}
export interface TicketAIAnalysisResult {
  ticketId: number;
  analysis: TicketAIAnalysis;
  suggestedCategoryId: number | null;
  suggestedPriorityId: number | null;
  provider: string;
  model: string;
  responseId: string;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
}
export interface TicketAIDraftResult {
  ticketId: number;
  draftText: string;
  provider: string;
  model: string;
  responseId: string;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
}
