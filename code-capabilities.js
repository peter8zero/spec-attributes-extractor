const CODE_CAPABILITIES = {
  gmp: [
    {
      id: "check_anti_franking",
      name: "Anti-Franking Check",
      description:
        "Checks whether excess pension above GMP is sufficient to cover GMP increases.",
      whyItMatters:
        "Prevents schemes from using GMP step-ups to reduce the total pension paid.",
      methodName: "CheckAntiFranking",
      returnType: "bool",
      parameters:
        "decimal totalPension, decimal gmpAmount, decimal gmpIncrease",
      parentOption: {
        id: "gmp_equaliser",
        name: "GMP Equalisation (Dual Record)",
      },
      codeClass: "GmpEqualiser",
      scheme: "Core",
      lastModified: "2025-12-01",
    },
    {
      id: "apply_section148",
      name: "Section 148 Revaluation",
      description:
        "Applies s148 orders to revalue GMP between leaving and GMP pension age.",
      methodName: "ApplySection148",
      returnType: "decimal",
      parameters: "decimal gmp, decimal revaluationFactor",
      parentOption: {
        id: "gmp_equaliser",
        name: "GMP Equalisation (Dual Record)",
      },
      codeClass: "GmpEqualiser",
      scheme: "Core",
      lastModified: "2025-12-01",
    },
  ],
  revaluation: [
    {
      id: "calculate_pro_rata",
      name: "Pro-rata CPI Revaluation",
      description: "Calculates CPI revaluation for a partial year period.",
      whyItMatters: "Needed when a member leaves mid-year.",
      methodName: "CalculateProRata",
      returnType: "decimal",
      parameters:
        "decimal pension, DateTime startDate, DateTime endDate, decimal partialFactor",
      parentOption: { id: "cpi_capped_revaluation", name: "CPI-Capped (s101)" },
      codeClass: "CpiCappedRevaluation",
      scheme: "Core",
      lastModified: "2025-11-15",
    },
  ],
  transfers: [
    {
      id: "calculate_partial_cetv",
      name: "Partial CETV",
      description: "Calculates a partial cash equivalent transfer value.",
      whyItMatters:
        "Needed when a member transfers only part of their benefits.",
      methodName: "CalculatePartialCetv",
      returnType: "decimal",
      parameters: "decimal totalCetv, decimal proportion",
      codeClass: "TransferCalculator",
      scheme: "Core",
      lastModified: "2025-11-20",
    },
    {
      id: "calculate_club_transfer",
      name: "Club Transfer Value",
      description:
        "Calculates transfer value under the Public Sector Transfer Club.",
      methodName: "CalculateClubTransfer",
      returnType: "decimal",
      parameters: "decimal pension, decimal clubFactor",
      codeClass: "TransferCalculator",
      scheme: "Core",
      lastModified: "2025-11-20",
    },
  ],
};
