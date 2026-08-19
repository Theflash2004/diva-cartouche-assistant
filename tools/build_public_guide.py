from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Image, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs" / "Diva-cartouche-assistant-guide.pdf"
LOGO = ROOT / "Assets" / "diva-cat-logo.png"
PURPLE = colors.HexColor("#7030A0")
LAVENDER = colors.HexColor("#F3ECFA")
DARK = colors.HexColor("#292335")

font_name = "Helvetica"
font_bold = "Helvetica-Bold"
for path in (Path(r"C:\Windows\Fonts\califb.ttf"), Path(r"C:\Windows\Fonts\calibrib.ttf")):
    if path.exists():
        try:
            pdfmetrics.registerFont(TTFont("DivaCalifornian", str(path)))
            font_name = "DivaCalifornian"
            break
        except Exception:
            pass

styles = getSampleStyleSheet()
styles.add(ParagraphStyle("Cover", fontName=font_name, fontSize=29, leading=34, textColor=PURPLE, alignment=TA_CENTER, spaceAfter=10))
styles.add(ParagraphStyle("Sub", fontName=font_name, fontSize=14, leading=20, textColor=DARK, alignment=TA_CENTER))
styles.add(ParagraphStyle("H", fontName=font_bold, fontSize=18, leading=23, textColor=PURPLE, spaceAfter=8))
styles.add(ParagraphStyle("BodyDiva", fontName=font_name, fontSize=11.5, leading=17, textColor=DARK, spaceAfter=7))
styles.add(ParagraphStyle("SmallDiva", fontName=font_name, fontSize=9.5, leading=13, textColor=DARK))
styles.add(ParagraphStyle("WhiteDiva", fontName=font_bold, fontSize=11, leading=15, textColor=colors.white, alignment=TA_CENTER))


def p(text, style="BodyDiva"):
    return Paragraph(text, styles[style])


def footer(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(PURPLE)
    canvas.setLineWidth(0.5)
    canvas.line(18 * mm, 14 * mm, 192 * mm, 14 * mm)
    canvas.setFont(font_name, 8)
    canvas.setFillColor(colors.HexColor("#6C6175"))
    canvas.drawString(18 * mm, 9 * mm, "Diva Cartouche Assistant")
    canvas.drawRightString(192 * mm, 9 * mm, f"{doc.page}")
    canvas.restoreState()


def ui_mockup():
    rows = [
        [p("Modèle", "SmallDiva"), p("Document (cartouche)", "SmallDiva")],
        [p("Titre du document", "SmallDiva"), p("Codification des documents", "SmallDiva")],
        [p("Type", "SmallDiva"), p("OUT - Outil / Guide", "SmallDiva")],
        [p("Domaine", "SmallDiva"), p("QUA - Qualité et Risques", "SmallDiva")],
        [p("Mot-clé", "SmallDiva"), p("Codification des documents", "SmallDiva")],
        [p("Version", "SmallDiva"), p("1", "SmallDiva")],
        [p("Préparé par", "SmallDiva"), p("Votre nom", "SmallDiva")],
        [p("", "SmallDiva"), p("CRÉER ET OUVRIR LE DOCUMENT", "WhiteDiva")],
    ]
    table = Table(rows, colWidths=[47 * mm, 116 * mm], rowHeights=[11 * mm] * len(rows))
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), colors.white),
        ("BACKGROUND", (1, -1), (1, -1), PURPLE),
        ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#D5C7DE")),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 7),
        ("RIGHTPADDING", (0, 0), (-1, -1), 7),
    ]))
    return table


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    doc = SimpleDocTemplate(str(OUTPUT), pagesize=A4, rightMargin=18 * mm, leftMargin=18 * mm, topMargin=18 * mm, bottomMargin=20 * mm)
    story = [
        Spacer(1, 16 * mm),
        Image(str(LOGO), width=42 * mm, height=42 * mm),
        Spacer(1, 8 * mm),
        p("Diva Cartouche Assistant", "Cover"),
        p("Guide rapide pour créer, codifier et classer vos documents Word", "Sub"),
        Spacer(1, 18 * mm),
        p("Une seule fenêtre : choisissez un modèle, complétez les informations, puis laissez Diva créer le fichier Word, le PDF et les dossiers parents.", "BodyDiva"),
        Spacer(1, 10 * mm),
        p("Version ARSEF", "SmallDiva"),
        PageBreak(),
        p("1. Créer un document", "H"),
        p("Remplissez les champs de haut en bas. Les listes Type et Domaine sont des menus contrôlés : elles évitent les variantes d'écriture qui rendent les recherches difficiles.", "BodyDiva"),
        ui_mockup(),
        Spacer(1, 8 * mm),
        p("Le chemin logique est : modèle -> titre -> classement -> mot-clé -> version -> création.", "SmallDiva"),
        Spacer(1, 8 * mm),
        p("Pour un email", "H"),
        p("Choisissez le modèle Email. Diva utilise automatiquement le type ENR, demande l'objet et le destinataire, puis crée une lettre avec la date et l'objet dans le document.", "BodyDiva"),
        PageBreak(),
        p("2. Nom et dossiers", "H"),
        p("Le nom suit la forme : TYPE-DOMAINE-MOT-CLÉ-VERSION. Le mot-clé peut être long et lisible, par exemple : OUT-QUA-Codification des documents-1.", "BodyDiva"),
        Table([
            [p("Bureau", "WhiteDiva"), p("Domaine", "WhiteDiva"), p("Type", "WhiteDiva"), p("Dossier du document", "WhiteDiva")],
            [p("ARSEF", "SmallDiva"), p("QUA", "SmallDiva"), p("OUTILS", "SmallDiva"), p("OUT-QUA-Codification des documents-1", "SmallDiva")],
        ], colWidths=[38 * mm, 31 * mm, 31 * mm, 63 * mm], rowHeights=[12 * mm, 18 * mm], style=TableStyle([
            ("BACKGROUND", (0, 0), (-1, 0), PURPLE), ("BACKGROUND", (0, 1), (-1, 1), LAVENDER),
            ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#D5C7DE")), ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ("LEFTPADDING", (0, 0), (-1, -1), 6), ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ])),
        Spacer(1, 10 * mm),
        p("Si le même nom existe déjà", "H"),
        p("Diva vous prévient et ne remplace rien. Pour une nouvelle version, changez Version. Pour un nouveau document, changez le Mot-clé de codification.", "BodyDiva"),
        p("Le PDF est créé à côté du DOCX. Quand le DOCX est modifié dans Word, Diva surveille le dossier et actualise le PDF automatiquement.", "BodyDiva"),
        PageBreak(),
        p("3. Installation et confidentialité", "H"),
        p("L'installateur crée un raccourci sur le vrai Bureau Windows, même si le Bureau est redirigé ou porte un autre nom. Diva crée le dossier ARSEF et ses dossiers parents automatiquement. L'application ne demande pas de compte et ne collecte aucune télémétrie.", "BodyDiva"),
        p("Les modèles ARSEF sont intégrés à l'application. Ils sont copiés dans %LOCALAPPDATA%\\DivaCartoucheAssistant\\Templates uniquement lorsque Word en a besoin.", "BodyDiva"),
        p("Mise à jour", "H"),
        p("Au démarrage, Diva peut vérifier la dernière release GitHub. Après votre accord, le paquet est téléchargé, vérifié par SHA-256, installé dans un dossier de staging, puis testé. Si le nouveau programme ne démarre pas, l'ancien est restauré. Les paramètres, modèles et schéma privés ne sont pas supprimés.", "BodyDiva"),
        Spacer(1, 16 * mm),
        p("Besoin d'aide ?", "H"),
        p("Fermez Word si un fichier reste verrouillé, relancez l'application, puis réessayez. Conservez toujours vos modèles privés dans un emplacement sauvegardé.", "BodyDiva"),
    ]
    doc.build(story, onFirstPage=footer, onLaterPages=footer)
    print(OUTPUT)


if __name__ == "__main__":
    build()
