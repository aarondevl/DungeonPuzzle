# Entregables de la Semana 04

| Archivo | Qué es |
|---|---|
| `DungeonPuzzle_Semana04_Sustentacion.pptx` | **El que hay que presentar.** 13 láminas, con las capturas del proyecto y notas de ponente en cada una. Generado desde `../PRESENTACION.md`. |
| `GrupoXX_*.pptx`, `*_v2.pptx` | Versiones anteriores hechas con otra herramienta. Se conservan como histórico. |

## Aviso sobre las versiones anteriores

Los `.pptx` antiguos tienen las cuatro duraciones de `SpikeTrap` **corridas una
posición** en su lámina de fases:

| Fase | Valor real en `SpikeTrap.cs` | Lo que dicen esos archivos |
|---|---|---|
| Oculta | 1.6 s | 0.25 s |
| Subiendo | 0.25 s | 1.6 s |
| Clavada | 0.9 s | 0.25 s |
| Bajando | 0.25 s | 0.9 s |

Vino de una ambigüedad en el diagrama de origen, ya corregida en
`PRESENTACION.md`. Si se recupera alguna de esas versiones, hay que arreglar esa
lámina antes de presentar.

## Regenerar el `.pptx`

El generador está en el historial de esta rama (`scratchpad/ppt/build.js` de la
sesión). Para rehacerlo desde cero, dale `../PRESENTACION.md` a la herramienta de
diapositivas que prefieras y pídele que respete los saltos de lámina.
