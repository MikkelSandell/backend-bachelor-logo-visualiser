# State Diagram — Product Configuration Lifecycle

Paste into **mermaid.live**

```mermaid
stateDiagram-v2
    [*] --> Oprettet : Admin opretter produkt

    Oprettet : Oprettet
    Oprettet : Ingen printzoner endnu

    ZonerTilfojet : Zoner tilføjet
    ZonerTilfojet : Har zoner men mangler fuldstændig metadata

    FuldtKonfigureret : Fuldt konfigureret
    FuldtKonfigureret : Alle zoner har navn, mm-mål og teknikker

    Oprettet --> ZonerTilfojet : Admin tegner zone på canvas
    ZonerTilfojet --> Oprettet : Alle zoner slettes

    ZonerTilfojet --> ZonerTilfojet : Zone tilføjet, redigeret eller slettet

    ZonerTilfojet --> FuldtKonfigureret : Gem ændringer med komplette zone-metadata

    FuldtKonfigureret --> ZonerTilfojet : Zone slettes eller metadata ufuldstændig

    FuldtKonfigureret --> FuldtKonfigureret : Zone opdateret og gemt
```
