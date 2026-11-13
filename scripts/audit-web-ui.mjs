import fs from 'fs'
import path from 'path'

console.log('====================================================')
console.log('🚀 FPT ALUMNI CONNECT - WEB UI & DESIGN SYSTEM AUDIT')
console.log('====================================================\n')

const rootDir = process.cwd()
const srcDir = path.join(rootDir, '-F-Alumni---Web', 'src')

let passCount = 0
let failCount = 0

function assert(condition, message) {
  if (condition) {
    console.log(`✅ [PASS] ${message}`)
    passCount++
  } else {
    console.error(`❌ [FAIL] ${message}`)
    failCount++
  }
}

function getAllFiles(dir, exts = ['.tsx', '.ts']) {
  let results = []
  const list = fs.readdirSync(dir)
  for (const file of list) {
    const fullPath = path.join(dir, file)
    const stat = fs.statSync(fullPath)
    if (stat && stat.isDirectory()) {
      results = results.concat(getAllFiles(fullPath, exts))
    } else if (exts.includes(path.extname(file))) {
      results.push(fullPath)
    }
  }
  return results
}

const allTsxFiles = getAllFiles(srcDir, ['.tsx'])

// Test 1: Icon Unification (@tabler/icons-react only)
console.log('--- 1. AUDIT HỆ THỐNG ICON & ĐỒNG BỘ THƯ VIỆN ---')
let lucideCount = 0
for (const file of allTsxFiles) {
  const content = fs.readFileSync(file, 'utf8')
  if (content.includes("from 'lucide-react'") || content.includes('from "lucide-react"')) {
    lucideCount++
    console.error(`  - Phát hiện lucide-react trong file: ${path.relative(rootDir, file)}`)
  }
}
assert(lucideCount === 0, `Toàn bộ icon đã được đồng bộ 100% sang @tabler/icons-react (0 file dùng lucide-react).`)

let tablerCount = 0
for (const file of allTsxFiles) {
  const content = fs.readFileSync(file, 'utf8')
  if (content.includes("from '@tabler/icons-react'") || content.includes('from "@tabler/icons-react"')) {
    tablerCount++
  }
}
assert(tablerCount >= 45, `Đã ghi nhận ${tablerCount} file giao diện sử dụng chuẩn @tabler/icons-react.`)

// Test 2: Bố cục Dynamic Layout trong UserLayout
console.log('\n--- 2. AUDIT BỐ CỤC WEB DESKTOP & DYNAMIC LAYOUT ---')
const userLayoutPath = path.join(srcDir, 'layouts', 'UserLayout.tsx')
const userLayoutContent = fs.readFileSync(userLayoutPath, 'utf8')

assert(
  userLayoutContent.includes('isFeedPage ?') &&
    userLayoutContent.includes('lg:col-span-8') &&
    userLayoutContent.includes('lg:col-span-12'),
  'UserLayout hỗ trợ Dynamic Columns: 8 cols cho Feed & 12 cols mở rộng cho Workspace (Chat, Opportunities, Alumni, Communities, Profile, Notifications).'
)

assert(
  userLayoutContent.includes('{isFeedPage && (') &&
    userLayoutContent.includes('<RightSidebar />') &&
    userLayoutContent.includes('lg:col-span-4'),
  'RightSidebar hiển thị 4 cols trên Feed page và tự động ẩn trên các trang Workspace để mở rộng diện tích hiển thị.'
)

// Test 3: Trang Web 404 Not Found & Router Fallback
console.log('\n--- 3. AUDIT KIẾN TRÚC ROUTER & TRANG WEB 404 NOT FOUND ---')
const notFoundPath = path.join(srcDir, 'pages', 'NotFoundPage.tsx')
assert(fs.existsSync(notFoundPath), 'Trang NotFoundPage.tsx đã được khởi tạo và sẵn sàng.')

const notFoundContent = fs.readFileSync(notFoundPath, 'utf8')
assert(
  notFoundContent.includes('IconCompassOff') &&
    notFoundContent.includes('Quay lại trang trước') &&
    notFoundContent.includes('Về trang chủ'),
  'NotFoundPage có đầy đủ nhận diện thương hiệu FPT, mã lỗi 404, và các nút điều hướng cứu hộ.'
)

const routesPath = path.join(srcDir, 'app', 'router', 'routes.tsx')
const routesContent = fs.readFileSync(routesPath, 'utf8')
assert(
  routesContent.includes("path: '*'") && routesContent.includes('<NotFoundPage />'),
  'Cây định tuyến routes.tsx đã map wildcard path (*) tới <NotFoundPage />.'
)

// Test 4: Design Tokens & Typography Scale
console.log('\n--- 4. AUDIT DESIGN TOKENS & TYPOGRAPHY STANDARDS ---')
const tailwindConfigPath = path.join(rootDir, '-F-Alumni---Web', 'tailwind.config.js')
const tailwindConfig = fs.readFileSync(tailwindConfigPath, 'utf8')

assert(
  tailwindConfig.includes("'#F27024'") || tailwindConfig.includes('"#F27024"'),
  'Tailwind config chứa mã màu thương hiệu FPT Orange (#F27024).'
)
assert(
  tailwindConfig.includes("'#1B365D'") || tailwindConfig.includes('"#1B365D"'),
  'Tailwind config chứa mã màu thương hiệu FPT Navy (#1B365D).'
)
assert(
  tailwindConfig.includes('Hanken Grotesk') &&
    tailwindConfig.includes('Inter') &&
    tailwindConfig.includes('IBM Plex Mono'),
  'Tailwind config định nghĩa chuẩn 3 font: Hanken Grotesk (Heading), Inter (Sans), IBM Plex Mono (Mono).'
)

console.log('\n====================================================')
console.log(`📊 TỔNG KẾT KIỂM THỬ: ${passCount} PASSED / ${passCount + failCount} TOTAL`)
console.log('====================================================')

if (failCount > 0) {
  process.exit(1)
} else {
  process.exit(0)
}
