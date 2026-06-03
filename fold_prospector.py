#!/usr/bin/env python3
"""
fold_prospector.py  --  mine the space of Mandelbox-family fold fractals.

Same idea as the attractor prospector: a low-dimensional, structured parameter
space (compositions of fold primitives + a scale), a cheap scoring funnel, and a
human-picked contact sheet at the end. The scorer is a FUNNEL, not a judge --
it culls the dead majority (dust, blobs, chaotic speckle) and ranks survivors so
your eye only has to look at coherent candidates. Final selection is yours.

The map (Mandelbrot convention, z0 = c = sample point):
    z -> scale * fold_k(...fold_2(fold_1(z))) + c

Fold primitives:
    box  L          reflect each component past +-L           (Mandelbox box fold)
    ball m f        invert inside the radius band [m, f]       (Mandelbox ball fold)
    abs             component-wise |z|                          (KIFS / Mandelbulb fold)
    rot R           fixed 3x3 rotation between folds            (AmazingBox family)

Validated: feeding (box 1.0, ball 0.5/1.0, scale 2.0) reproduces the Mandelbox;
(scale -1.5) the AmazingBox. See foldengine_sanity.png.

A found genome maps straight to a Parsec DE core: the op list becomes the
per-iteration fold sequence in estimate(), `scale` is the affine multiplier, and
the running-scalar DE is dr -> |scale|*(product of fold linear factors)*dr + 1,
DE = |z|/dr (the Mandelbox/KIFS linear DE, NOT the z^2 log form).
"""
import numpy as np
from scipy import ndimage

# ----------------------------- fold engine ---------------------------------
def box_fold(v, L):
    return np.clip(v, -L, L) * 2.0 - v

def ball_fold(v, m, f):
    r2 = np.sum(v*v, 1, keepdims=True); m2, f2 = m*m, f*f
    fac = np.ones_like(r2)
    fac = np.where(r2 < m2, f2/m2, fac)
    fac = np.where((r2 >= m2) & (r2 < f2), f2/np.maximum(r2, 1e-12), fac)
    return v * fac

def rot_matrix(a, b, c):
    cx, sx = np.cos(a), np.sin(a); cy, sy = np.cos(b), np.sin(b); cz, sz = np.cos(c), np.sin(c)
    return (np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]]) @
            np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]]) @
            np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]]))

def apply_op(v, op):
    t = op[0]
    if t == 'box':  return box_fold(v, op[1])
    if t == 'ball': return ball_fold(v, op[1], op[2])
    if t == 'abs':  return np.abs(v)
    if t == 'rot':  return v @ op[1].T
    return v

def escape_field(genome, res, box, slice_z=0.0, maxiter=26, bail=6.0):
    """Escape-time on a 2D slice (z = slice_z) of the 3D fold set."""
    ops, scale = genome
    xs = np.linspace(-box, box, res); X, Y = np.meshgrid(xs, xs)
    C = np.stack([X.ravel(), Y.ravel(), np.full(X.size, slice_z)], 1)
    Z = C.copy(); N = len(C)
    esc = np.full(N, maxiter, float); alive = np.ones(N, bool)
    for i in range(maxiter):
        r = np.sqrt(np.sum(Z*Z, 1)); out = (r > bail) & alive
        esc[out] = i; alive &= ~out
        if not alive.any(): break
        Zi = Z[alive]
        for op in ops: Zi = apply_op(Zi, op)
        Z[alive] = scale * Zi + C[alive]
    return esc.reshape(res, res)

# ----------------------------- scoring funnel -------------------------------
def boxcount_dim(mask):
    H, W = mask.shape; sizes = [2, 4, 8, 16, 32]; cs = []
    for s in sizes:
        h = (H//s)*s; w = (W//s)*s
        cs.append(max(int(mask[:h,:w].reshape(h//s, s, w//s, s).any(axis=(1,3)).sum()), 1))
    return -np.polyfit(np.log(sizes), np.log(cs), 1)[0]

def frame_extent(genome, maxiter=26):
    e = escape_field(genome, 64, 10.0, maxiter=maxiter); b = e >= maxiter
    if b.sum() < 8: return None
    xs = np.linspace(-10, 10, 64); X, Y = np.meshgrid(xs, xs)
    return float(np.clip(max(np.abs(X[b]).max(), np.abs(Y[b]).max())*1.18, 1.0, 11.0))

# Tunable thresholds -- the knobs that shape what the mine surfaces.
FRAC_MIN, FRAC_MAX = 0.02, 0.40   # reject dust (<min) and solid blobs (>max)
COH_MIN            = 0.45         # reject disconnected speckle/dust
EDGE_MIN           = 40           # need a real boundary

def score(genome, maxiter=26, res=180):
    ext = frame_extent(genome, maxiter)
    if ext is None: return None
    e = escape_field(genome, res, ext, maxiter=maxiter); b = e >= maxiter
    frac = b.mean()
    if frac < FRAC_MIN or frac > FRAC_MAX: return None
    bc = ndimage.binary_opening(b, iterations=1)          # de-speckle: kills static
    if bc.sum() < 60: return None
    lab, n = ndimage.label(bc)
    coh = (np.bincount(lab.ravel())[1:].max() / bc.sum()) if n else 0.0
    if coh < COH_MIN: return None                          # must be coherent
    nb = ~bc
    edge = bc & (np.roll(nb,1,0)|np.roll(nb,-1,0)|np.roll(nb,1,1)|np.roll(nb,-1,1))
    if edge.sum() < EDGE_MIN: return None
    dim     = boxcount_dim(edge)
    speckle = 1.0 - bc.sum()/max(b.sum(), 1)               # 0 = clean, high = noisy
    edged   = edge.mean()
    # blend: reward boundary richness + coherence + low speckle + boundary not core
    blend = dim * coh * (1.0 - 0.6*speckle) * (0.5 + edged)
    return dict(dim=float(dim), frac=float(frac), coh=float(coh),
                speckle=float(speckle), edged=float(edged), ext=ext, blend=float(blend))

# ----------------------------- genome sampler -------------------------------
def random_genome(rng):
    k = rng.integers(2, 5); ops = []
    for _ in range(k):
        t = rng.choice(['box','ball','abs','rot'], p=[0.34, 0.34, 0.16, 0.16])
        if t == 'box':   ops.append(('box', float(rng.uniform(0.6, 1.8))))
        elif t == 'ball':
            m = float(rng.uniform(0.3, 0.8)); ops.append(('ball', m, float(rng.uniform(m+0.1, 1.5))))
        elif t == 'abs': ops.append(('abs',))
        else:            ops.append(('rot', rot_matrix(*rng.uniform(-1.2, 1.2, 3))))
    if not any(o[0] in ('box','ball') for o in ops):       # guarantee a bounding fold
        ops.append(('box', float(rng.uniform(0.8, 1.4))))
    return (ops, float(rng.uniform(1.3, 3.0)) * (1 if rng.random() < 0.6 else -1))

def genome_name(g):
    ops, s = g
    fmt = {'box': lambda o: f"box{o[1]:.2f}", 'ball': lambda o: f"ball{o[1]:.2f}/{o[2]:.2f}",
           'abs': lambda o: "abs", 'rot': lambda o: "rot"}
    return " ".join(fmt[o[0]](o) for o in ops) + f"  s={s:.2f}"

def supersample(genome, ext, res=300, ss=2, maxiter=46):
    e = escape_field(genome, res*ss, ext, maxiter=maxiter)
    return e.reshape(res, ss, res, ss).mean(axis=(1, 3))   # AA: speckle self-averages

# ----------------------------- main mine ------------------------------------
def mine(n=460, seed=77, top=8, out="prospector_hits.png"):
    rng = np.random.default_rng(seed)
    hits = []
    for _ in range(n):
        g = random_genome(rng); sc = score(g)
        if sc: hits.append((sc['blend'], g, sc))
    hits.sort(key=lambda h: -h[0])
    print(f"{n} sampled, {len(hits)} passed the funnel. Top {top}:")
    for bl, g, sc in hits[:top]:
        print(f"  blend={bl:.2f} dim={sc['dim']:.2f} coh={sc['coh']:.2f} "
              f"speckle={sc['speckle']:.2f} | {genome_name(g)}")
    import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
    sel = hits[:top]; cols = 4; rows = (len(sel)+cols-1)//cols
    fig, axs = plt.subplots(rows, cols, figsize=(4*cols, 4.2*rows), facecolor='#0a0a12')
    for ax, (bl, g, sc) in zip(np.array(axs).ravel(), sel):
        ax.imshow(supersample(g, sc['ext']), cmap='magma', origin='lower'); ax.axis('off')
        ax.set_title(f"blend={bl:.2f} dim={sc['dim']:.2f}\n{genome_name(g)}", color='#ccc', fontsize=8)
    fig.suptitle("Fold-fractal prospector", color='#eee', fontsize=14)
    plt.tight_layout(); plt.savefig(out, dpi=100, facecolor='#0a0a12')
    return hits

if __name__ == "__main__":
    mine()
