import os
from PIL import Image, ImageDraw, ImageFont

art_dir = r"C:\Users\Family\.gemini\antigravity\brain\54ba6702-7025-4d0e-8453-7e8a31665ba5"

# Load Fonts
font_title = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 20)
font_subtitle = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 15)
font_h1 = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 17)
font_body = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 14)
font_bold = ImageFont.truetype(r"C:\Windows\Fonts\segoeuib.ttf", 14)
font_small = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 12)
font_badge = ImageFont.truetype(r"C:\Windows\Fonts\segoeuib.ttf", 11)

def draw_rounded_rect(draw, bbox, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(bbox, radius=radius, fill=fill, outline=outline, width=width)

def draw_toggle_switch(draw, x, y, state=True):
    w, h = 44, 22
    if state:
        draw_rounded_rect(draw, (x, y, x + w, y + h), 11, fill="#1B4D3E", outline="#2ED573", width=1)
        draw.ellipse((x + w - 19, y + 2, x + w - 3, y + 18), fill="#2ED573")
    else:
        draw_rounded_rect(draw, (x, y, x + w, y + h), 11, fill="#21262D", outline="#30363D", width=1)
        draw.ellipse((x + 3, y + 2, x + 19, y + 18), fill="#8B949E")

# =========================================================================
# VARIANT 1: Cyber-Modern Sidebar (Современный игровой чит-клиент)
# =========================================================================
def create_variant_1():
    img = Image.new("RGBA", (1060, 700), (10, 14, 20, 255))
    draw = ImageDraw.Draw(img)

    for y in range(0, 700, 28):
        draw.line([(0, y), (1060, y)], fill=(18, 24, 34, 100), width=1)
    for x in range(0, 1060, 28):
        draw.line([(x, 0), (x, 700)], fill=(18, 24, 34, 100), width=1)

    win_x, win_y, win_w, win_h = 30, 30, 1000, 640
    for g in range(4, 0, -1):
        draw_rounded_rect(draw, (win_x - g, win_y - g, win_x + win_w + g, win_y + win_h + g), 14, fill=None, outline=(46, 213, 115, 20 * g), width=1)
    draw_rounded_rect(draw, (win_x, win_y, win_x + win_w, win_y + win_h), 12, fill="#141822", outline="#2ED573", width=2)

    # Header
    draw_rounded_rect(draw, (win_x, win_y, win_x + win_w, win_y + 48), 12, fill="#0F121A")
    draw.rectangle((win_x, win_y + 30, win_x + win_w, win_y + 48), fill="#0F121A")
    draw.line([(win_x, win_y + 48), (win_x + win_w, win_y + 48)], fill="#232A3B", width=1)

    draw.text((win_x + 18, win_y + 12), "CUSTOM MENU", fill="#2ED573", font=font_h1)
    draw_rounded_rect(draw, (win_x + 155, win_y + 14, win_x + 208, win_y + 34), 4, fill="#1F6FEB")
    draw.text((win_x + 162, win_y + 16), "v1.7.0", fill="#FFFFFF", font=font_badge)
    draw.text((win_x + win_w - 250, win_y + 15), "Зажмите шапку для перемещения ✥", fill="#6C757D", font=font_small)

    # Sidebar Navigation
    side_w = 210
    draw.rectangle((win_x, win_y + 49, win_x + side_w, win_y + win_h - 1), fill="#0F121A")
    draw.line([(win_x + side_w, win_y + 49), (win_x + side_w, win_y + win_h - 1)], fill="#232A3B", width=1)

    nav = [
        ("⚡  ТВИКИ И ЧИТЫ", True),
        ("📦  СПАВНЕР ПРЕДМЕТОВ", False),
        ("⌨  ГОРЯЧИЕ КЛАВИШИ", False),
        ("⚙  НАСТРОЙКИ МОДА", False),
    ]
    cur_y = win_y + 65
    for name, act in nav:
        if act:
            draw_rounded_rect(draw, (win_x + 10, cur_y, win_x + side_w - 10, cur_y + 42), 8, fill="#153629", outline="#2ED573", width=1)
            draw.text((win_x + 22, cur_y + 11), name, fill="#2ED573", font=font_bold)
        else:
            draw_rounded_rect(draw, (win_x + 10, cur_y, win_x + side_w - 10, cur_y + 42), 8, fill="#141822")
            draw.text((win_x + 22, cur_y + 11), name, fill="#8B949E", font=font_body)
        cur_y += 50

    # User Profile card
    draw_rounded_rect(draw, (win_x + 10, win_y + win_h - 65, win_x + side_w - 10, win_y + win_h - 12), 6, fill="#141822", outline="#232A3B")
    draw.ellipse((win_x + 20, win_y + win_h - 52, win_x + 44, win_y + win_h - 28), fill="#2ED573")
    draw.text((win_x + 50, win_y + win_h - 54), "Pattern (Хост)", fill="#FFFFFF", font=font_bold)
    draw.text((win_x + 50, win_y + win_h - 36), "100 HP  •  0% Радиации", fill="#8B949E", font=font_badge)

    # Content Column 1: Cheats
    cx = win_x + side_w + 20
    cy = win_y + 65
    col1_w = 360
    draw.text((cx, cy), "СПОСОБНОСТИ ПЕРСОНАЖА", fill="#2ED573", font=font_subtitle)

    cheats = [
        ("Режим Бога (GodMode)", "Бессмертие, защита от всех атак", True, "[F1]"),
        ("Бесконечный бег", "Выносливость не тратится", True, "[F2]"),
        ("Без штрафа веса", "Максимальная скорость бега", True, "[F3]"),
        ("Бесшумные шаги", "Монстры не слышат шагов", False, "—"),
        ("Вечный фонарик / Батарея", "100% заряд и прочность предметов", True, "—"),
        ("Анти-радиация", "Защита от дождя и заражения", True, "—"),
    ]
    row_y = cy + 28
    for title, desc, st, key in cheats:
        draw_rounded_rect(draw, (cx, row_y, cx + col1_w, row_y + 54), 8, fill="#1A202C", outline="#2D3748")
        draw.text((cx + 12, row_y + 8), title, fill="#FFFFFF", font=font_bold)
        draw.text((cx + 12, row_y + 28), desc, fill="#718096", font=font_small)
        draw_toggle_switch(draw, cx + col1_w - 52, row_y + 15, state=st)
        row_y += 60

    # Content Column 2: Visuals (ESP & Lighting)
    cx2 = cx + col1_w + 20
    col2_w = 360
    draw.text((cx2, cy), "ПОДСВЕТКА И ВИЗУАЛЫ", fill="#2ED573", font=font_subtitle)

    # ESP Items
    card_y = cy + 28
    draw_rounded_rect(draw, (cx2, card_y, cx2 + col2_w, card_y + 124), 8, fill="#1A202C", outline="#2D3748")
    draw.text((cx2 + 12, card_y + 8), "Подсветка предметов (Item ESP)", fill="#FFFFFF", font=font_bold)
    draw_toggle_switch(draw, cx2 + col2_w - 52, card_y + 8, state=True)
    draw.text((cx2 + 12, card_y + 34), "Дистанция: 40м  [25м] [50м] [75м] [100м]", fill="#A0AEC0", font=font_small)
    # Slider
    draw_rounded_rect(draw, (cx2 + 12, card_y + 56, cx2 + col2_w - 12, card_y + 64), 4, fill="#2D3748")
    draw_rounded_rect(draw, (cx2 + 12, card_y + 56, cx2 + 160, card_y + 64), 4, fill="#2ED573")
    draw.ellipse((cx2 + 155, card_y + 52, cx2 + 171, card_y + 68), fill="#FFFFFF")
    # Palette
    colors = ["#FFD933", "#33F266", "#33D9FF", "#D966FF", "#FF8C26", "#FFFFFF", "#FF5973"]
    px = cx2 + 12
    for c in colors:
        draw.ellipse((px, card_y + 78, px + 22, card_y + 100), fill=c)
        px += 30
    draw.text((cx2 + 12, card_y + 104), "Превью: ● Золотой слиток [$120] (14м)", fill="#FFD933", font=font_badge)

    # ESP Actors
    card_y2 = card_y + 134
    draw_rounded_rect(draw, (cx2, card_y2, cx2 + col2_w, card_y2 + 115), 8, fill="#1A202C", outline="#2D3748")
    draw.text((cx2 + 12, card_y2 + 8), "ESP игроков и монстров", fill="#FFFFFF", font=font_bold)
    draw_toggle_switch(draw, cx2 + col2_w - 52, card_y2 + 8, state=True)
    draw.text((cx2 + 12, card_y2 + 34), "Дистанция: 100м  [50м] [100м] [150м] [200м]", fill="#A0AEC0", font=font_small)
    draw_rounded_rect(draw, (cx2 + 12, card_y2 + 56, cx2 + col2_w - 12, card_y2 + 64), 4, fill="#2D3748")
    draw_rounded_rect(draw, (cx2 + 12, card_y2 + 56, cx2 + 200, card_y2 + 64), 4, fill="#2ED573")
    draw.ellipse((cx2 + 195, card_y2 + 52, cx2 + 211, card_y2 + 68), fill="#FFFFFF")
    draw_rounded_rect(draw, (cx2 + 12, card_y2 + 76, cx2 + 165, card_y2 + 100), 4, fill="#153629", outline="#2ED573")
    draw.text((cx2 + 20, card_y2 + 80), "✔ Союзники (Зелёный)", fill="#2ED573", font=font_badge)
    draw_rounded_rect(draw, (cx2 + 175, card_y2 + 76, cx2 + 315, card_y2 + 100), 4, fill="#381D20", outline="#FF4757")
    draw.text((cx2 + 185, card_y2 + 80), "✔ Враги (Красный)", fill="#FF4757", font=font_badge)

    # FullBright
    card_y3 = card_y2 + 125
    draw_rounded_rect(draw, (cx2, card_y3, cx2 + col2_w, card_y3 + 92), 8, fill="#1A202C", outline="#2D3748")
    draw.text((cx2 + 12, card_y3 + 8), "FullBright (Ночное зрение)", fill="#FFFFFF", font=font_bold)
    draw_toggle_switch(draw, cx2 + col2_w - 52, card_y3 + 8, state=True)
    draw.text((cx2 + 12, card_y3 + 34), "Яркость: 1.2x  [0.8x] [1.2x] [2.0x] [3.0x]", fill="#A0AEC0", font=font_small)
    draw_rounded_rect(draw, (cx2 + 12, card_y3 + 58, cx2 + col2_w - 12, card_y3 + 66), 4, fill="#2D3748")
    draw_rounded_rect(draw, (cx2 + 12, card_y3 + 58, cx2 + 140, card_y3 + 66), 4, fill="#2ED573")
    draw.ellipse((cx2 + 135, card_y3 + 54, cx2 + 151, card_y3 + 70), fill="#FFFFFF")

    # Bottom Actions
    bar_y = win_y + win_h - 55
    draw_rounded_rect(draw, (cx, bar_y, cx + 170, bar_y + 36), 6, fill="#238636")
    draw.text((cx + 36, bar_y + 8), "Включить всё", fill="#FFFFFF", font=font_bold)
    draw_rounded_rect(draw, (cx + 180, bar_y, cx + 350, bar_y + 36), 6, fill="#DA3633")
    draw.text((cx + 210, bar_y + 8), "Выключить всё", fill="#FFFFFF", font=font_bold)
    draw_rounded_rect(draw, (cx2 + 120, bar_y, cx2 + col2_w, bar_y + 36), 6, fill="#2D3748")
    draw.text((cx2 + 155, bar_y + 8), "Закрыть меню [F7]", fill="#FFFFFF", font=font_bold)

    out = os.path.join(art_dir, "ui_variant_1_cyber.png")
    img.save(out, "PNG")
    print("Saved Variant 1")

# =========================================================================
# VARIANT 2: Minimalist Grid / Floating Dock (Компактные плитки)
# =========================================================================
def create_variant_2():
    img = Image.new("RGBA", (1060, 700), (12, 14, 18, 255))
    draw = ImageDraw.Draw(img)

    for y in range(0, 700, 24):
        draw.line([(0, y), (1060, y)], fill=(20, 24, 30, 80), width=1)
    for x in range(0, 1060, 24):
        draw.line([(x, 0), (x, 700)], fill=(20, 24, 30, 80), width=1)

    win_x, win_y, win_w, win_h = 70, 40, 920, 620
    draw_rounded_rect(draw, (win_x, win_y, win_x + win_w, win_y + win_h), 16, fill="#161A22", outline="#303846", width=2)

    # Top Floating Header with Tabs
    draw_rounded_rect(draw, (win_x + 16, win_y + 14, win_x + win_w - 16, win_y + 56), 10, fill="#0F131A")
    draw.text((win_x + 30, win_y + 22), "CUSTOM MENU", fill="#2ED573", font=font_h1)

    # Tabs in header
    tx = win_x + 230
    tab_list = [("Твики и Читы", True), ("Спавн предметов", False), ("Клавиши и Бинды", False)]
    for tname, tact in tab_list:
        tw = 160
        if tact:
            draw_rounded_rect(draw, (tx, win_y + 18, tx + tw, win_y + 52), 8, fill="#1B382B", outline="#2ED573")
            draw.text((tx + 28, win_y + 26), tname, fill="#2ED573", font=font_bold)
        else:
            draw_rounded_rect(draw, (tx, win_y + 18, tx + tw, win_y + 52), 8, fill="#161A22")
            draw.text((tx + 28, win_y + 26), tname, fill="#8B949E", font=font_body)
        tx += tw + 10

    # Grid of Cards (3 Columns x 3 Rows)
    grid_x = win_x + 24
    grid_y = win_y + 75
    cw = 280
    ch = 110
    gap = 14

    items = [
        ("Режим Бога", "Бессмертие от всех атак", True, "F1", "#2ED573"),
        ("Бесконечный бег", "Бег без усталости", True, "F2", "#2ED573"),
        ("Без штрафа веса", "Скорость 100% при любом весе", True, "F3", "#2ED573"),
        ("Бесшумные шаги", "Монстры не реагируют", False, "None", "#8B949E"),
        ("Вечный фонарик", "Батарея не садится", True, "None", "#2ED573"),
        ("Анти-радиация", "Иммунитет к дождю", True, "None", "#2ED573"),
        ("FullBright", "Регулировка яркости 1.2x", True, "F4", "#2ED573"),
        ("ESP предметов", "Сквозь стены (40м)", True, "F5", "#2ED573"),
        ("ESP игроков и врагов", "Союзники & Враги (100м)", True, "F6", "#2ED573"),
    ]

    idx = 0
    for r in range(3):
        for c in range(3):
            ix = grid_x + c * (cw + gap)
            iy = grid_y + r * (ch + gap)
            title, desc, st, key, col = items[idx]
            idx += 1

            # Card box
            draw_rounded_rect(draw, (ix, iy, ix + cw, iy + ch), 10, fill="#1D222D", outline="#2A3241")
            # LED status light
            led_col = "#2ED573" if st else "#4A5568"
            draw.ellipse((ix + 12, iy + 14, ix + 24, iy + 26), fill=led_col)
            draw.text((ix + 32, iy + 10), title, fill="#FFFFFF", font=font_bold)
            draw_toggle_switch(draw, ix + cw - 52, iy + 10, state=st)

            draw.text((ix + 12, iy + 42), desc, fill="#8B949E", font=font_small)

            # Keybind Pill at bottom of card
            draw_rounded_rect(draw, (ix + 12, iy + 74, ix + 100, iy + 98), 5, fill="#141820", outline="#2A3241")
            draw.text((ix + 20, iy + 77), f"Бросок: {key}", fill="#718096" if key == "None" else "#2ED573", font=font_badge)

            draw_rounded_rect(draw, (ix + cw - 70, iy + 74, ix + cw - 12, iy + 98), 5, fill="#242B38")
            draw.text((ix + cw - 56, iy + 77), "Опции", fill="#A0AEC0", font=font_badge)

    # Bottom Quick Item Spawner Shortcut Bar
    quick_y = win_y + win_h - 110
    draw_rounded_rect(draw, (win_x + 24, quick_y, win_x + win_w - 24, quick_y + 45), 8, fill="#1B222E", outline="#2ED573")
    draw.text((win_x + 36, quick_y + 12), "⚡ БЫСТРЫЙ СПАВН ПО КНОПКЕ:", fill="#2ED573", font=font_bold)
    draw_rounded_rect(draw, (win_x + 280, quick_y + 8, win_x + 470, quick_y + 36), 6, fill="#161B24", outline="#2A3241")
    draw.text((win_x + 290, quick_y + 12), "📦 Сок HP (38%) [В руки]", fill="#FFFFFF", font=font_small)
    draw_rounded_rect(draw, (win_x + 480, quick_y + 8, win_x + 560, quick_y + 36), 6, fill="#238636")
    draw.text((win_x + 494, quick_y + 12), "Бинд: [N1]", fill="#FFFFFF", font=font_bold)
    draw.text((win_x + 580, quick_y + 13), "Нажмите N1 в игре чтобы мгновенно создать!", fill="#A0AEC0", font=font_small)

    # Footer Buttons
    fy = win_y + win_h - 50
    draw_rounded_rect(draw, (win_x + 24, fy, win_x + 190, fy + 36), 6, fill="#238636")
    draw.text((win_x + 50, fy + 8), "Включить всё", fill="#FFFFFF", font=font_bold)
    draw_rounded_rect(draw, (win_x + 205, fy, win_x + 370, fy + 36), 6, fill="#DA3633")
    draw.text((win_x + 230, fy + 8), "Выключить всё", fill="#FFFFFF", font=font_bold)
    draw_rounded_rect(draw, (win_x + win_w - 210, fy, win_x + win_w - 24, fy + 36), 6, fill="#2A3241")
    draw.text((win_x + win_w - 180, fy + 8), "Закрыть [F7]", fill="#FFFFFF", font=font_bold)

    out = os.path.join(art_dir, "ui_variant_2_grid.png")
    img.save(out, "PNG")
    print("Saved Variant 2")

# =========================================================================
# VARIANT 3: Pro Studio Dashboard (3 Колонки на одном экране)
# =========================================================================
def create_variant_3():
    img = Image.new("RGBA", (1080, 700), (9, 12, 16, 255))
    draw = ImageDraw.Draw(img)

    for y in range(0, 700, 20):
        draw.line([(0, y), (1080, y)], fill=(16, 20, 28, 90), width=1)
    for x in range(0, 1080, 20):
        draw.line([(x, 0), (x, 700)], fill=(16, 20, 28, 90), width=1)

    win_x, win_y, win_w, win_h = 25, 25, 1030, 650
    draw_rounded_rect(draw, (win_x, win_y, win_x + win_w, win_y + win_h), 12, fill="#131720", outline="#1F6FEB", width=2)

    # Top Info Bar
    draw_rounded_rect(draw, (win_x, win_y, win_x + win_w, win_y + 44), 12, fill="#0D1117")
    draw.rectangle((win_x, win_y + 25, win_x + win_w, win_y + 44), fill="#0D1117")
    draw.line([(win_x, win_y + 44), (win_x + win_w, win_y + 44)], fill="#21262D", width=1)

    draw.text((win_x + 16, win_y + 11), "MIMESIS STUDIO // CUSTOM MENU", fill="#1F6FEB", font=font_bold)
    draw.text((win_x + 320, win_y + 12), "● СЕССИЯ: АКТИВНА  |  ПИНГ: 12мс  |  ИГРОК: Pattern  |  ХП: 100/100", fill="#8B949E", font=font_small)
    draw.text((win_x + win_w - 180, win_y + 12), "ГОРЯЧАЯ КЛАВИША: [F7]", fill="#2ED573", font=font_badge)

    # 3 Separate Equal Columns
    col_w = 320
    gap = 14
    c1_x = win_x + 16
    c2_x = c1_x + col_w + gap
    c3_x = c2_x + col_w + gap
    content_y = win_y + 56
    col_h = win_h - 110

    # Column 1: Твики персонажа
    draw_rounded_rect(draw, (c1_x, content_y, c1_x + col_w, content_y + col_h), 10, fill="#181D26", outline="#252D3D")
    draw_rounded_rect(draw, (c1_x, content_y, c1_x + col_w, content_y + 36), 10, fill="#1E2532")
    draw.rectangle((c1_x, content_y + 20, c1_x + col_w, content_y + 36), fill="#1E2532")
    draw.text((c1_x + 14, content_y + 8), "1. ТВИКИ ПЕРСОНАЖА", fill="#2ED573", font=font_bold)

    cheats = [
        ("Режим Бога (GodMode)", True),
        ("Бесконечный бег", True),
        ("Без штрафа веса", True),
        ("Бесшумные шаги", False),
        ("Вечный фонарик", True),
        ("Анти-радиация", True),
        ("HUD статуса команды", True),
    ]
    cy = content_y + 46
    for t, st in cheats:
        draw_rounded_rect(draw, (c1_x + 10, cy, c1_x + col_w - 10, cy + 42), 6, fill="#202734")
        draw.text((c1_x + 20, cy + 11), t, fill="#FFFFFF", font=font_body)
        draw_toggle_switch(draw, c1_x + col_w - 56, cy + 10, state=st)
        cy += 48

    # Column 2: Визуальные функции & ESP
    draw_rounded_rect(draw, (c2_x, content_y, c2_x + col_w, content_y + col_h), 10, fill="#181D26", outline="#252D3D")
    draw_rounded_rect(draw, (c2_x, content_y, c2_x + col_w, content_y + 36), 10, fill="#1E2532")
    draw.rectangle((c2_x, content_y + 20, c2_x + col_w, content_y + 36), fill="#1E2532")
    draw.text((c2_x + 14, content_y + 8), "2. ESP И ОСВЕЩЕНИЕ", fill="#1F6FEB", font=font_bold)

    # Item ESP box
    ey = content_y + 46
    draw_rounded_rect(draw, (c2_x + 10, ey, c2_x + col_w - 10, ey + 130), 6, fill="#202734")
    draw.text((c2_x + 18, ey + 8), "Подсветка предметов", fill="#FFFFFF", font=font_bold)
    draw_toggle_switch(draw, c2_x + col_w - 56, ey + 8, state=True)
    draw.text((c2_x + 18, ey + 34), "Дальность: 40м [25] [50] [75] [100]", fill="#8B949E", font=font_small)
    draw_rounded_rect(draw, (c2_x + 18, ey + 54, c2_x + col_w - 18, ey + 60), 3, fill="#2D3748")
    draw_rounded_rect(draw, (c2_x + 18, ey + 54, c2_x + 140, ey + 60), 3, fill="#FFD933")
    colors = ["#FFD933", "#33F266", "#33D9FF", "#D966FF", "#FF8C26", "#FFFFFF", "#FF5973"]
    px = c2_x + 18
    for c in colors:
        draw.ellipse((px, ey + 72, px + 20, ey + 92), fill=c)
        px += 28
    draw.text((c2_x + 18, ey + 102), "● Золотой слиток [$120] (14м)", fill="#FFD933", font=font_badge)

    # Actor ESP box
    ey2 = ey + 138
    draw_rounded_rect(draw, (c2_x + 10, ey2, c2_x + col_w - 10, ey2 + 110), 6, fill="#202734")
    draw.text((c2_x + 18, ey2 + 8), "ESP союзников & врагов", fill="#FFFFFF", font=font_bold)
    draw_toggle_switch(draw, c2_x + col_w - 56, ey2 + 8, state=True)
    draw.text((c2_x + 18, ey2 + 34), "● Союзники: Зеленый   ■ Враги: Красный", fill="#8B949E", font=font_small)
    draw.text((c2_x + 18, ey2 + 56), "Дальность: 100м [50] [100] [150] [200]", fill="#8B949E", font=font_small)
    draw_rounded_rect(draw, (c2_x + 18, ey2 + 76, c2_x + col_w - 18, ey2 + 82), 3, fill="#2D3748")
    draw_rounded_rect(draw, (c2_x + 18, ey2 + 76, c2_x + 180, ey2 + 82), 3, fill="#2ED573")

    # Fullbright box
    ey3 = ey2 + 118
    draw_rounded_rect(draw, (c2_x + 10, ey3, c2_x + col_w - 10, ey3 + 82), 6, fill="#202734")
    draw.text((c2_x + 18, ey3 + 8), "FullBright (Свет)", fill="#FFFFFF", font=font_bold)
    draw_toggle_switch(draw, c2_x + col_w - 56, ey3 + 8, state=True)
    draw.text((c2_x + 18, ey3 + 34), "Яркость: 1.2x [0.8] [1.2] [2.0] [3.0]", fill="#8B949E", font=font_small)
    draw_rounded_rect(draw, (c2_x + 18, ey3 + 54, c2_x + col_w - 18, ey3 + 60), 3, fill="#2D3748")
    draw_rounded_rect(draw, (c2_x + 18, ey3 + 54, c2_x + 130, ey3 + 60), 3, fill="#1F6FEB")

    # Column 3: Спавнер предметов & Бинды
    draw_rounded_rect(draw, (c3_x, content_y, c3_x + col_w, content_y + col_h), 10, fill="#181D26", outline="#252D3D")
    draw_rounded_rect(draw, (c3_x, content_y, c3_x + col_w, content_y + 36), 10, fill="#1E2532")
    draw.rectangle((c3_x, content_y + 20, c3_x + col_w, content_y + 36), fill="#1E2532")
    draw.text((c3_x + 14, content_y + 8), "3. СПАВНЕР И БИНДЫ", fill="#FF9F43", font=font_bold)

    # Search bar
    sy = content_y + 46
    draw_rounded_rect(draw, (c3_x + 10, sy, c3_x + col_w - 10, sy + 32), 6, fill="#0F131A", outline="#252D3D")
    draw.text((c3_x + 20, sy + 6), "🔍 Поиск предметов...", fill="#718096", font=font_small)

    # Item List Mini
    item_rows = [
        ("Сок HP (38%)", "#3105", "Расходник"),
        ("Тестовая коробка", "#3001", "Расходник"),
        ("Тоник выносливости", "#3201", "Расходник"),
        ("Фонарик (Pro)", "#2011", "Оборудование"),
    ]
    iy = sy + 38
    for iname, iid, itype in item_rows:
        draw_rounded_rect(draw, (c3_x + 10, iy, c3_x + col_w - 10, iy + 36), 6, fill="#202734")
        draw.text((c3_x + 18, iy + 8), iname, fill="#FFFFFF", font=font_small)
        draw.text((c3_x + col_w - 85, iy + 8), itype, fill="#718096", font=font_badge)
        iy += 40

    # Selected Item Card & Keybind Assignment
    iy += 4
    draw_rounded_rect(draw, (c3_x + 10, iy, c3_x + col_w - 10, iy + 140), 6, fill="#1E2634", outline="#FF9F43")
    draw.text((c3_x + 18, iy + 8), "Выбран: Сок HP (38%)", fill="#FF9F43", font=font_bold)
    draw.text((c3_x + 18, iy + 28), "ID: 3105  •  Вес: 2.0 кг", fill="#A0AEC0", font=font_badge)

    draw_rounded_rect(draw, (c3_x + 18, iy + 48, c3_x + 155, iy + 76), 4, fill="#238636")
    draw.text((c3_x + 26, iy + 53), "Спавн в руки", fill="#FFFFFF", font=font_badge)
    draw_rounded_rect(draw, (c3_x + 165, iy + 48, c3_x + col_w - 18, iy + 76), 4, fill="#30363D")
    draw.text((c3_x + 175, iy + 53), "На землю", fill="#FFFFFF", font=font_badge)

    draw_rounded_rect(draw, (c3_x + 18, iy + 86, c3_x + col_w - 18, iy + 126), 6, fill="#141922", outline="#252D3D")
    draw.text((c3_x + 26, iy + 92), "⚡ Хоткей: [ Num 1 ] (В руки)", fill="#2ED573", font=font_bold)
    draw.text((c3_x + 26, iy + 108), "Нажмите Num 1 для спавна", fill="#718096", font=font_badge)

    # Footer
    fy = win_y + win_h - 45
    draw_rounded_rect(draw, (c1_x, fy, c1_x + 150, fy + 32), 6, fill="#238636")
    draw.text((c1_x + 24, fy + 6), "Включить всё", fill="#FFFFFF", font=font_bold)
    draw_rounded_rect(draw, (c1_x + 160, fy, c1_x + 310, fy + 32), 6, fill="#DA3633")
    draw.text((c1_x + 184, fy + 6), "Выключить всё", fill="#FFFFFF", font=font_bold)

    draw_rounded_rect(draw, (c3_x + col_w - 140, fy, c3_x + col_w, fy + 32), 6, fill="#30363D")
    draw.text((c3_x + col_w - 124, fy + 6), "Закрыть [F7]", fill="#FFFFFF", font=font_bold)

    out = os.path.join(art_dir, "ui_variant_3_dashboard.png")
    img.save(out, "PNG")
    print("Saved Variant 3")

create_variant_1()
create_variant_2()
create_variant_3()
print("All 3 UI Mockup images generated successfully!")
