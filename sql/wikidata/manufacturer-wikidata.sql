SELECT DISTINCT
       ?manufacturer
       ?manufacturerLabel
WHERE {
  ?aircraft wdt:P176 ?manufacturer .
  ?aircraft wdt:P31/wdt:P279* wd:Q11436 .

  ?manufacturer rdfs:label ?manufacturerLabel .
  FILTER(LANG(?manufacturerLabel) = "en")
}
ORDER BY ?manufacturerLabel
