# Temporário: procura no Commons fotos com o NOME do jogador e o CLUBE ATUAL juntos (título, descrição ou categorias).
import json, os, re, time, urllib.parse, urllib.request
UA = "BraSumula/1.0 (https://brasumula.com.br; https://github.com/mariaclara7/api-sumula)"
CM = "https://commons.wikimedia.org/w/api.php"
def get(url, params=None, raw=False):
    if params: url += "?" + urllib.parse.urlencode(params)
    with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": UA}), timeout=60) as r:
        d = r.read(); return d if raw else json.loads(d)
def api(**p):
    p.update(format="json", formatversion="2"); time.sleep(0.3); return get(CM, p)
ALVOS = [
  (37833, "Carlos Vinícius", ["Carlos Vinícius", "Carlos Vinicius"], ["Grêmio", "Gremio"]),
  (42901, "Luciano", ["Luciano Neves", "Luciano"], ["São Paulo FC", "São Paulo", "Sao Paulo"]),
  (149704, "Danilo", ["Danilo Santos", "Danilo dos Santos", "Danilo"], ["Botafogo"]),
  (157533, "John Kennedy", ["John Kennedy"], ["Fluminense"]),
  (45490, "Matheus Pereira", ["Matheus Pereira"], ["Cruzeiro"]),
  (140873, "Luciano Juba", ["Luciano Juba", "Juba"], ["Bahia"]),
  (178710, "Samuel Lino", ["Samuel Lino"], ["Flamengo"]),
]
saida = []
for jid, nome, nomes, clubes in ALVOS:
    titulos = set()
    for n in nomes:
        for c in clubes[:2]:
            for q in (f'intitle:"{n}" "{c}"', f'"{n}" "{c}"'):
                resp = api(action="query", list="search", srnamespace=6, srlimit=50, srsearch=q)
                if "query" not in resp: print("  busca falhou:", q, resp.get("error")); continue
                for r in resp["query"]["search"]:
                    titulos.add(r["title"])
    titulos = sorted(titulos); achados = []
    for i in range(0, len(titulos), 40):
        r = api(action="query", titles="|".join(titulos[i:i+40]), prop="imageinfo|categories", clshow="!hidden", cllimit=500,
                iiprop="url|extmetadata|timestamp", iiurlwidth=360)
        for p in r["query"]["pages"]:
            if "imageinfo" not in p: continue
            ii = p["imageinfo"][0]; meta = ii.get("extmetadata", {})
            cats = " ".join(c["title"] for c in p.get("categories", []))
            desc = re.sub("<[^>]+>", " ", meta.get("ImageDescription", {}).get("value", ""))
            texto = f"{p['title']} {cats} {desc}".lower()
            tem_nome = any(n.lower() in texto for n in nomes[:2]) if len(nomes) > 1 else nomes[0].lower() in texto
            tem_clube = any(c.lower() in texto for c in clubes)
            if not (tem_nome and tem_clube): continue
            data = re.sub("<[^>]+>", "", meta.get("DateTimeOriginal", {}).get("value", "") or ii.get("timestamp", ""))
            ano = re.search(r"(20\d\d)", data)
            achados.append({"titulo": p["title"], "ano": int(ano.group(1)) if ano else 0, "thumb": ii.get("thumburl"), "pagina": ii.get("descriptionurl"),
                            "autor": re.sub(r"\s+", " ", re.sub("<[^>]+>", " ", meta.get("Artist", {}).get("value", ""))).strip()[:80],
                            "licenca": meta.get("LicenseShortName", {}).get("value", ""), "descricao": desc.strip()[:200]})
    achados.sort(key=lambda x: x["ano"], reverse=True)
    pasta = f"precisa/{jid}"; os.makedirs(pasta, exist_ok=True)
    for n, x in enumerate(achados[:8]):
        x["arquivo"] = f"{pasta}/{n:02d}.jpg"; open(x["arquivo"], "wb").write(get(x["thumb"], raw=True)); time.sleep(0.3)
    print(nome, len(titulos), "->", len(achados))
    saida.append({"id": jid, "nome": nome, "achados": achados[:8]})
json.dump(saida, open("precisa/indice.json", "w"), ensure_ascii=False, indent=2)
