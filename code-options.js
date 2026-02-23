const CODE_OPTIONS = {
  commutation: [
    {
      id: "trivial_commutation",
      name: "Trivial Commutation",
      description:
        "Full commutation of small pots below the trivial commutation limit.",
      whyItMatters:
        "Allows members with very small benefits to take a one-off lump sum instead of a pension.",
      codeClass: "TrivialCommutation",
      scheme: "Core",
      lastModified: "2025-09-20",
    },
  ],
  gmp: [
    {
      id: "gmp_equaliser",
      name: "GMP Equalisation (Dual Record)",
      description:
        "Applies GMP equalisation using the dual-record method per Lloyds.",
      whyItMatters:
        "Required for schemes with members who have GMP service between 1990-1997.",
      codeClass: "GmpEqualiser",
      scheme: "Core",
      lastModified: "2025-12-01",
    },
  ],
  revaluation: [
    {
      id: "cpi_capped_revaluation",
      name: "CPI-Capped (s101)",
      description: "Statutory revaluation capped at CPI rather than RPI.",
      whyItMatters:
        "The standard for post-97 deferred benefits under most modern schemes.",
      codeClass: "CpiCappedRevaluation",
      scheme: "Core",
      lastModified: "2025-11-15",
    },
    {
      id: "fixed_rate_revaluation",
      name: "Fixed Rate",
      description:
        "Revaluation at a fixed annual rate specified in scheme rules.",
      codeClass: "FixedRateRevaluation",
      scheme: "Core",
      lastModified: "2025-10-01",
    },
  ],
};
