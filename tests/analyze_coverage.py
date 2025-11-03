#!/usr/bin/env python3
"""カバレッジXMLからドメインロジック (Services/Utils/Models) の統計を抽出"""
import xml.etree.ElementTree as ET
import sys

def analyze_coverage(xml_path):
    tree = ET.parse(xml_path)
    root = tree.getroot()
    
    # 全体統計
    total_lines = int(root.attrib['lines-valid'])
    total_covered = int(root.attrib['lines-covered'])
    total_rate = float(root.attrib['line-rate'])
    
    # ドメインクラス集計
    domain_lines = 0
    domain_covered = 0
    domain_classes = []
    
    # UI/非ドメイン集計
    ui_lines = 0
    ui_covered = 0
    
    for cls in root.findall('.//class'):
        filename = cls.attrib.get('filename', '')
        classname = cls.attrib.get('name', '')
        line_rate = float(cls.attrib.get('line-rate', 0))
        
        # クラスごとの行数計算
        lines = cls.findall('.//line')
        class_lines = len(lines)
        class_covered = sum(1 for line in lines if int(line.attrib.get('hits', 0)) > 0)
        
        # ドメイン判定 (Services, Utils, Models)
        is_domain = any(x in filename for x in ['Services\\', 'Utils\\', 'Models\\'])
        
        if is_domain:
            domain_lines += class_lines
            domain_covered += class_covered
            domain_classes.append({
                'name': classname,
                'file': filename,
                'rate': line_rate,
                'lines': class_lines,
                'covered': class_covered
            })
        elif any(x in filename for x in ['UI\\', 'Program.cs']):
            ui_lines += class_lines
            ui_covered += class_covered
    
    # 結果出力
    print("=" * 70)
    print("カバレッジ分析結果")
    print("=" * 70)
    print(f"\n【全体統計】")
    print(f"  総行数:        {total_lines:>6} 行")
    print(f"  カバー済:      {total_covered:>6} 行")
    print(f"  カバレッジ率:  {total_rate*100:>6.2f}%")
    
    print(f"\n【UI/Program (除外推奨)】")
    print(f"  総行数:        {ui_lines:>6} 行")
    print(f"  カバー済:      {ui_covered:>6} 行")
    if ui_lines > 0:
        print(f"  カバレッジ率:  {ui_covered/ui_lines*100:>6.2f}%")
    
    print(f"\n【ドメインロジック (Services/Utils/Models)】")
    print(f"  総行数:        {domain_lines:>6} 行")
    print(f"  カバー済:      {domain_covered:>6} 行")
    if domain_lines > 0:
        domain_rate = domain_covered / domain_lines
        print(f"  カバレッジ率:  {domain_rate*100:>6.2f}%")
    
    # 除外後の実質カバレッジ
    adjusted_lines = total_lines - ui_lines
    adjusted_covered = total_covered - ui_covered
    if adjusted_lines > 0:
        adjusted_rate = adjusted_covered / adjusted_lines
        print(f"\n【実質カバレッジ (UI除外後)】")
        print(f"  総行数:        {adjusted_lines:>6} 行")
        print(f"  カバー済:      {adjusted_covered:>6} 行")
        print(f"  カバレッジ率:  {adjusted_rate*100:>6.2f}%")
    
    # クラス別詳細 (カバレッジ低い順)
    print(f"\n【ドメインクラス詳細 (カバレッジ昇順)】")
    domain_classes.sort(key=lambda x: x['rate'])
    for cls in domain_classes[:15]:  # 低い方から15件
        print(f"  {cls['rate']*100:>6.2f}% | {cls['covered']:>4}/{cls['lines']:<4} | {cls['name']}")
    
    print("=" * 70)

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: python analyze_coverage.py <coverage.cobertura.xml>")
        sys.exit(1)
    
    analyze_coverage(sys.argv[1])
