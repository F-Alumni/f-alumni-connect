/* Rebuild the offline SVG, HTML, Mermaid and editable diagrams.net documents.
 * No network access or third-party packages required. Run: node generate-diagrams.cjs
 */
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../..');
const out = __dirname;
const esc = s => String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
const read = p => fs.readFileSync(p,'utf8');
const save = (p,s) => fs.writeFileSync(path.join(out,p),s,'utf8');
const colors = { Forum:'#155e75', AI:'#4338ca', line:'#64748b', ink:'#0f172a' };
function entityData(service) {
  const base = path.join(root,`f-alumni-${service.toLowerCase()}-service`,'src',`${service === 'AI'?'Ai':service}Service.Repositories`);
  const config = read(path.join(base,'Configurations',`${service === 'AI'?'Ai':service}Configuration.cs`)) + read(path.join(base,'Configurations','SampleItemConfiguration.cs'));
  const entities=[];
  for(const file of fs.readdirSync(path.join(base,'Entities')).filter(f=>f.endsWith('.cs'))) {
    const src=read(path.join(base,'Entities',file));
    const starts=[...src.matchAll(/public class (\w+)/g)];
    starts.forEach((match,i)=>{
      const body=src.slice(match.index,starts[i+1]?.index ?? src.length);
      const props=[...body.matchAll(/public ([\w?<>]+) (\w+) \{ get; set; \}/g)].map(m=>({type:m[1],name:m[2]}));
      const cm=config.match(new RegExp(`void Configure\\(EntityTypeBuilder<${match[1]}> builder\\)([\\s\\S]*?)(?=public void Configure|$)`))?.[1] ?? '';
      const table=cm.match(/ToTable\("(\w+)"\)/)?.[1] ?? match[1];
      const scalar=props.filter(p=>!p.type.includes('ICollection') && /^(Guid|DateTime|string|int|bool)/.test(p.type));
      scalar.forEach(p=>{
        const property=cm.match(new RegExp(`builder\\.Property\\([^;]*?=>[^;]*?\\.${p.name}\\)[^;]*;`))?.[0] ?? '';
        const max=property.match(/HasMaxLength\((\d+)\)/)?.[1];
        p.sql = p.type.startsWith('Guid')?'uniqueidentifier':p.type.startsWith('DateTime')?'datetime2':p.type.startsWith('bool')?'bit':p.type.startsWith('int')?'int':`nvarchar(${max??'max'})`;
        p.nullable=p.type.endsWith('?');
      });
      entities.push({name:match[1],table,props,scalar});
    });
  }
  return entities;
}
const relations={Forum:[['Post','PostImage','PostId','Cascade'],['Post','PostTag','PostId','Cascade'],['Post','Comment','PostId','Cascade'],['Post','Reaction','PostId','Cascade'],['Post','Bookmark','PostId','Cascade'],['Comment','Comment','ParentCommentId','Restrict'],['Event','EventAttendee','EventId','Cascade']],AI:[['RagDocument','RagChunk','DocumentId','Cascade'],['AiConversation','AiMessage','ConversationId','Cascade']]};
class Page {
  constructor(name,title,subtitle,w,h,accent){Object.assign(this,{name,title,subtitle,w,h,accent});this.nodes=[];this.edges=[];this.texts=[];this.id=0;}
  box(id,title,lines,x,y,w=440,kind='class') {const n={id,title,lines,x,y,w,h:57+lines.length*24,kind};this.nodes.push(n);return n;}
  edge(from,to,label='',options={}){this.edges.push({from,to,label,...options});}
  note(text,x,y,w=1000){this.nodes.push({id:`note${this.id++}`,title:'',lines:text.split('\n'),x,y,w,h:22+text.split('\n').length*24,kind:'note'});}
  label(text,x,y,size=18){this.texts.push({text,x,y,size});}
  point(n,side){return side==='left'?[n.x,n.y+n.h/2]:side==='top'?[n.x+n.w/2,n.y]:side==='bottom'?[n.x+n.w/2,n.y+n.h]:[n.x+n.w,n.y+n.h/2];}
  route(e){if(e.points)return e.points;const a=this.nodes.find(n=>n.id===e.from),b=this.nodes.find(n=>n.id===e.to);if(!a||!b)throw Error('Unknown edge '+JSON.stringify(e));const sideA=e.sideA??(a.x<b.x?'right':'left');const sideB=e.sideB??(a.x<b.x?'left':'right');const p=this.point(a,sideA),q=this.point(b,sideB);return [p,[((p[0]+q[0])/2),p[1]],[((p[0]+q[0])/2),q[1]],q];}
  svg(){
    const nodes=this.nodes.map(n=>{
      const note=n.kind==='note';
      return `<g><rect x="${n.x}" y="${n.y}" width="${n.w}" height="${n.h}" rx="${note?8:4}" fill="${note?'#fff7ed':'white'}" stroke="${note?'#fdba74':'#cbd5e1'}"/>${note?'':`<path d="M${n.x+4} ${n.y}H${n.x+n.w-4}Q${n.x+n.w} ${n.y} ${n.x+n.w} ${n.y+4}V${n.y+40}H${n.x}V${n.y+4}Q${n.x} ${n.y} ${n.x+4} ${n.y}" fill="${this.accent}"/><text x="${n.x+16}" y="${n.y+27}" font-size="19" font-weight="650" fill="white">${esc(n.title)}</text>`}${n.lines.map((l,i)=>`<text x="${n.x+14}" y="${n.y+(note?28:64)+i*24}" font-size="${note?16:16}" font-family="${note?'Segoe UI, sans-serif':'Consolas, monospace'}" fill="${note?'#9a3412':colors.ink}">${esc(l)}</text>`).join('')}</g>`;
    }).join('');
    const edges=this.edges.map(e=>{
      const p=this.route(e),last=p.at(-1),first=p[0];
      const mid=e.labelAt??[(p[1][0]+p[p.length-2][0])/2,(p[1][1]+p[p.length-2][1])/2];
      return `<g><polyline points="${p.map(p=>p.join(',')).join(' ')}" fill="none" stroke="${e.color??colors.line}" stroke-width="1.7" ${e.dashed?'stroke-dasharray="7 5"':''} ${e.arrow===false?'':`marker-end="url(#${e.realize?'triangle':e.many&&!e.uml?'many':'arrow'})"`} ${e.many&&!e.uml?`marker-start="url(#${e.optional?'zeroone':'one'})"`:''}/>${e.label?`<text x="${mid[0]+(e.dx??(first[0]===last[0]?55:0))}" y="${mid[1]+(e.dy??-9)}" text-anchor="${e.anchor??'middle'}" font-size="15" fill="${colors.ink}" paint-order="stroke" stroke="white" stroke-width="5" stroke-linejoin="round">${esc(e.label)}</text>`:''}${e.many&&e.uml?`<text x="${first[0]+(p[1][0]<first[0]?-30:8)}" y="${first[1]-10}" font-size="14" fill="#475569">${e.optional?'0..1':'1'}</text><text x="${last[0]+(p[p.length-2][0]>last[0]?8:-45)}" y="${last[1]+(p[p.length-2][1]===last[1]?20:-10)}" font-size="14" fill="#475569">0..*</text>`:''}</g>`;
    }).join('');
    return `<svg xmlns="http://www.w3.org/2000/svg" width="${this.w}" height="${this.h}" viewBox="0 0 ${this.w} ${this.h}" role="img" aria-label="${esc(this.title)}"><defs><marker id="arrow" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0 0 L9 4 L0 8" fill="none" stroke="#64748b" stroke-width="1.3"/></marker><marker id="triangle" markerWidth="14" markerHeight="12" refX="13" refY="6" orient="auto"><path d="M0 0 L13 6 L0 12Z" fill="white" stroke="#64748b"/></marker><marker id="zeroone" markerWidth="18" markerHeight="16" refX="18" refY="8" orient="auto-start-reverse"><circle cx="5" cy="8" r="3" fill="white" stroke="#64748b"/><path d="M12 1V15" stroke="#64748b"/></marker><marker id="one" markerWidth="14" markerHeight="16" refX="14" refY="8" orient="auto-start-reverse"><path d="M6 1V15 M10 1V15" fill="none" stroke="#64748b"/></marker><marker id="many" markerWidth="20" markerHeight="18" refX="20" refY="9" orient="auto"><circle cx="4" cy="9" r="3" fill="white" stroke="#64748b"/><path d="M9 9L20 1 M9 9L20 9 M9 9L20 17" stroke="#64748b" fill="none"/></marker></defs><rect width="100%" height="100%" fill="#f8fafc"/><g font-family="Segoe UI, Arial, sans-serif"><text x="35" y="47" font-size="29" font-weight="700" fill="${this.accent}">${esc(this.title)}</text><text x="35" y="79" font-size="16" fill="#475569">${esc(this.subtitle)}</text>${edges}${nodes}${this.texts.map(t=>`<text x="${t.x}" y="${t.y}" font-size="${t.size}" fill="#475569">${esc(t.text)}</text>`).join('')}<text x="35" y="${this.h-20}" font-size="14" fill="#64748b">FPT Alumni Connect · Source snapshot: 11 Nov 2026 · ${esc(this.name)}</text></g></svg>`;
  }
  xml(){
    const cell=(id,value,style,geometry,edge='')=>`<mxCell id="${esc(id)}" value="${esc(value)}" style="${esc(style)}" ${edge||'vertex="1"'} parent="1">${geometry}</mxCell>`;
    const geo=(x,y,w,h)=>`<mxGeometry x="${x}" y="${y}" width="${w}" height="${h}" as="geometry"/>`;
    let cells=cell('title',this.title,'text;align=left;fontSize=28;fontStyle=1;fontColor='+this.accent,geo(35,15,this.w-70,40));
    cells+=cell('subtitle',this.subtitle,'text;align=left;fontSize=16;fontColor=#475569',geo(35,60,this.w-70,35));
    for(const n of this.nodes){
      if(n.kind==='note'){cells+=cell(n.id,n.lines.join('\n'),'rounded=1;whiteSpace=wrap;html=0;align=left;verticalAlign=top;spacing=12;fillColor=#fff7ed;strokeColor=#fdba74;fontColor=#9a3412;fontSize=16',geo(n.x,n.y,n.w,n.h));continue;}
      cells+=cell(n.id,'','rounded=0;fillColor=#ffffff;strokeColor=#cbd5e1',geo(n.x,n.y,n.w,n.h));
      cells+=cell(n.id+'head',n.title,`rounded=0;fillColor=${this.accent};strokeColor=${this.accent};fontColor=#ffffff;fontSize=19;fontStyle=1;align=left;spacingLeft=14`,geo(n.x,n.y,n.w,40));
      cells+=cell(n.id+'body',n.lines.join('\n'),'text;html=0;whiteSpace=wrap;align=left;verticalAlign=top;spacingLeft=14;spacingTop=9;fontSize=16;fontFamily=Consolas',geo(n.x,n.y+40,n.w,n.h-40));
    }
    this.edges.forEach((e,i)=>{
      const p=this.route(e);const first=p[0],last=p.at(-1);
      const style=`edgeStyle=none;rounded=0;html=0;strokeColor=#64748b;fontSize=15;labelBackgroundColor=#ffffff;endArrow=${e.arrow===false?'none':e.realize?'block':e.many&&!e.uml?'ERzeroToMany':'open'};endFill=0;${e.dashed?'dashed=1;':''}${e.many&&!e.uml?`startArrow=${e.optional?'ERzeroToOne':'ERmandOne'};startFill=0;`:''}`;
      cells+=cell('edge'+i,e.label,style,`<mxGeometry relative="1" as="geometry"><mxPoint x="${first[0]}" y="${first[1]}" as="sourcePoint"/><mxPoint x="${last[0]}" y="${last[1]}" as="targetPoint"/><Array as="points">${p.slice(1,-1).map(q=>`<mxPoint x="${q[0]}" y="${q[1]}"/>`).join('')}</Array></mxGeometry>`,'edge="1"');
    });
    this.edges.forEach((e,i)=>{
      if(!e.many || !e.uml) return;
      const p=this.route(e), a=p[0], b=p.at(-1);
      const ax=a[0]+(p[1][0]<a[0]?-35:5), ay=a[1]-28;
      const bx=b[0]+(p[p.length-2][0]>b[0]?5:-45), by=b[1]+(p[p.length-2][1]===b[1]?2:-28);
      const style='text;align=left;fontSize=14;fontColor=#475569;fillColor=#f8fafc';
      cells+=cell('cardA'+i,e.optional?'0..1':'1',style,geo(ax,ay,40,22));
      cells+=cell('cardB'+i,'0..*',style,geo(bx,by,40,22));
    });
    this.texts.forEach((t,i)=>{cells+=cell('text'+i,t.text,`text;align=left;fontSize=${t.size};fontColor=#475569`,geo(t.x,t.y-22,this.w-t.x-30,28));});
    return `<diagram name="${esc(this.name)}" id="${esc(this.name)}"><mxGraphModel dx="1200" dy="900" grid="1" gridSize="10" page="1" pageScale="1" pageWidth="${this.w}" pageHeight="${this.h}" background="#f8fafc"><root><mxCell id="0"/><mxCell id="1" parent="0"/>${cells}</root></mxGraphModel></diagram>`;
  }
}
function sequence(service) {
  const forum=service==='Forum';
  const p=new Page('01 Sequence',`${service} · Sequence Diagram`,forum?'Tạo bài viết: POST /api/v1/posts · authenticated user':'Hỏi đáp RAG: POST /api/v1/ai/chat · JWT hoặc Guest',1640,forum?1350:1760,colors[service]);
  const names=forum?['User / Web','PostsController','PostService','PostRepository','Forum SQL Server']:['AiChatController','AiChatService','RagSearchService','RagRepository','Google Gemini'];
  const xs=[165,485,805,1125,1445];
  names.forEach((n,i)=>{p.box('actor'+i,n,[],xs[i]-145,135,290);p.edge('','', '',{points:[[xs[i],192],[xs[i],p.h-280]],dashed:true,arrow:false});});
  let y=250,index=0;
  const msg=(a,b,label,ret=false)=>{p.edge('','',`${++index}. ${label}`,{points:[[xs[a],y],[xs[b],y]],labelAt:[(xs[a]+xs[b])/2,y],anchor:'middle',dx:0,dy:-12,dashed:ret});y+=66;};
  const note=(text)=>{p.note(text,50,y-14,1540);y+=text.split('\n').length*24+52;};
  if(forum){
    msg(0,1,'POST /api/v1/posts + Bearer JWT');
    note('ASP.NET Core: validate JWT + request model. Invalid token → 401; invalid body → 400.\nController lấy userId, fullName, avatarUrl từ claims.');
    msg(1,2,'CreatePostAsync(request, authorId, name, avatar)');
    note('PostService: trim title/content; tạo PostImages theo thứ tự; chuẩn hóa PostTags.');
    msg(2,3,'AddAsync(Post + Images + Tags)');
    msg(3,4,'DbContext.Posts.AddAsync + SaveChangesAsync');
    msg(4,3,'Lưu aggregate + khóa ngoại nội bộ',true);
    msg(3,2,'Post đã lưu',true);
    note('PostService gọi IKafkaProducerService.PublishPostCreatedAsync qua _ = (không await).\nTopic post.created.v1; key PostId; lỗi publish được log. Không bảo đảm delivery bằng outbox.');
    msg(2,1,'PostResponse',true);
    note('PostsController await SignalR: Clients.All.SendAsync("ReceiveNewPost", created).\nTác vụ Kafka có thể vẫn đang chạy khi HTTP response được trả về.');
    msg(1,0,'201 Created + Location + PostResponse',true);
  }else{
    note('Entry: controller lấy userId từ JWT; nếu chưa đăng nhập dùng Guest GUID chung.\nRagRepository đọc/ghi SQL Server qua AiServiceDbContext; DB được gộp vào lane repository.');
    msg(0,1,'AskAsync(request, userId)');
    msg(1,3,'GetConversationAsync(id, userId) nếu có id');
    msg(3,1,'Conversation + lịch sử, hoặc null',true);
    msg(1,3,'CreateConversationAsync nếu không tìm thấy');
    msg(1,2,'SearchRelevantChunksAsync(question, major, campus, 4)');
    msg(2,3,'GetAllChunksAsync(programCode, campus)');
    msg(3,2,'Chunks + RagDocument',true);
    msg(2,1,'Chấm điểm từ khóa + intent; Score > 5; Top 4',true);
    note('AiChatService tạo citations từ nguồn. Nhánh Gemini chỉ chạy khi có API key và có chunks.');
    msg(1,4,'[key + chunks] POST generateContent + context');
    msg(4,1,'[success] generated answer',true);
    note('Fallback: thiếu key, không có chunks, API lỗi hoặc câu trả lời rỗng → tổng hợp local.\nKhông có chunks → thông báo chưa có tài liệu liên quan. History hiện chưa được chèn vào prompt.');
    msg(1,3,'AddMessageAsync(user) → SaveChanges');
    msg(1,3,'AddMessageAsync(assistant + CitationsJson) → SaveChanges');
    note('Mỗi AddMessageAsync cập nhật LastMessageAt và tự SaveChangesAsync riêng.\nTin nhắn được lưu sau khi sinh câu trả lời; đây không phải một transaction chung cho cả 2 tin.');
    msg(1,0,'AiChatResponse: conversationId, answer, citations → HTTP 200',true);
  }
  p.h=Math.max(p.h,y+140);
  return p;
}
const forumPos={Post:[610,130,510],PostImage:[30,130,460],PostTag:[30,410,460],Comment:[1240,130,520],Reaction:[1240,700,520],Bookmark:[30,750,460],Event:[610,1090,510],EventAttendee:[1240,1190,520],Category:[30,1190,460],Report:[30,1580,460],SampleItem:[1240,1670,520]};
const aiPos={RagDocument:[40,140,630],RagChunk:[930,140,680],AiConversation:[40,650,630],AiMessage:[930,650,680],MentorProfile:[40,1140,630],SampleItem:[930,1140,680]};
function domain(service,entities,erd){
  const p=new Page(erd?'04 ERD':'03 Class - Domain',`${service} · ${erd?'Entity Relationship Diagram':'Domain Class Diagram'}`,erd?'Model EF Core hiện tại · PK / FK · kiểu SQL Server · không truy vấn database đang chạy':'C# entities + navigation properties · + public · association multiplicities',service==='Forum'?1830:1660,service==='Forum'?2280:1790,colors[service]);
  const pos=service==='Forum'?forumPos:aiPos;
  entities.forEach(e=>{
    const fks=relations[service].filter(r=>r[1]===e.name).map(r=>r[2]);
    const lines=erd?e.scalar.map(q=>`${q.name==='Id'?'PK':fks.includes(q.name)?'FK':'  '} ${q.name}: ${q.sql}${q.nullable?' NULL':''}`):e.props.map(q=>`+ ${q.name}: ${q.type.replace('ICollection','Collection')}`);
    const [x,y,w]=pos[e.name];
    p.box(e.name,erd?e.table:e.name,lines,x,y,w);
  });
  for(const [a,b,fk,del] of relations[service]){
    const an=p.nodes.find(n=>n.id===a),bn=p.nodes.find(n=>n.id===b);
    let options={many:true,uml:!erd,optional:fk==='ParentCommentId'};
    if(a===b){const x=an.x+an.w,y=an.y; options.points=[[x,y+100],[x+38,y+100],[x+38,y-25],[an.x+an.w/2,y-25],[an.x+an.w/2,y]];options.labelAt=[an.x+an.w/2,y-25];options.dx=0;options.dy=-7;options.anchor='middle';}
    else if(service==='Forum'&&a==='Post'){
      const left=bn.x<an.x;const ax=left?an.x:an.x+an.w;const bx=left?bn.x+bn.w:bn.x;
      const ay=an.y+an.h*{PostImage:0.2,PostTag:0.55,Comment:0.25,Reaction:0.85,Bookmark:0.95}[b];
      const by=bn.y+90;const channel=left?550:1180;
      options.points=[[ax,ay],[channel,ay],[channel,by],[bx,by]];options.labelAt=[channel,Math.min(ay,by)];options.dx=0;options.dy=-12;options.anchor='middle';
    }
    if(a==='Event'){options.labelAt=[1180,Math.min(an.y+an.h/2,bn.y+bn.h/2)];options.dx=0;options.anchor='middle';options.dy=-18;}
    if(service==='AI'){options.labelAt=[800,Math.min(an.y+an.h/2,bn.y+bn.h/2)];options.dx=0;options.anchor='middle';options.dy=-18;}
    p.edge(a,b,fk,options);
  }
  if(service==='Forum'){
    p.note('Delete Cascade cho tất cả FK, ngoại trừ Comments.ParentCommentId: Restrict.\nKhông có FK Posts → Categories. Report.TargetId + TargetType là tham chiếu logic; không có FK tới Post/Comment.\nAuthorId / UserId / ReporterId / CreatorId là GUID ngoài service, không tạo FK sang User DB.\nUNIQUE: Categories.Slug; Reactions(PostId, UserId); Bookmarks(PostId, UserId); EventAttendees(EventId, UserId).',30,2100,1730);
  }else{
    p.note('UserId trong AiConversations và MentorProfiles là tham chiếu logic tới User Service; không có cross-database FK.\nCitationsJson chứa JSON, không có FK từ AiMessages tới RagChunks. SampleItems là bảng template độc lập.\nQuan hệ 1 → 0..*: RagDocuments / RagChunks và AiConversations / AiMessages; delete Cascade.',40,1630,1570);
  }
  return p;
}
function core(service){
  const f=service==='Forum';const p=new Page('02 Class - Application',`${service} · Application Class Diagram`,'Dependency qua interface · realization nét đứt + tam giác · các method là tập con tiêu biểu',1780,f?1840:1640,colors[service]);
  const add=(id,lines,x,y,w=350,title=id)=>p.box(id,title,lines,x,y,w);
  if(f){
    add('PostsController',['+ Create()','+ GetFeed()','+ GetById()','+ Update() / Delete()'],30,140);
    add('PostService',['+ CreatePostAsync()','+ GetFeedAsync()','+ GetPostByIdAsync()','+ UpdatePostAsync()','+ DeletePostAsync()'],480,140);
    add('IPostService',['<<interface>>'],480,400);
    add('IPostRepository',['<<interface>>','+ AddAsync()','+ GetByIdAsync()','+ GetFeedAsync()'],940,140);
    add('PostRepository',['+ AddAsync()','+ GetByIdAsync()','+ GetFeedAsync()'],1380,140);
    add('CommentsController',['+ AddComment() / GetByPostId()'],30,590);
    add('CommentService',['+ AddCommentAsync()','+ AcceptAnswerAsync()'],480,590);
    add('ICommentRepository',['<<interface>>'],940,590);
    add('CommentRepository',['repository implementation'],1380,590);
    add('ReactionsController',['ReactionsController','BookmarksController','ReportsController','CategoriesController'],30,870,350,'Interaction controllers');
    add('InteractionService',['+ ToggleReactionAsync()','+ ToggleBookmarkAsync()','+ CreateReportAsync()','+ GetCategoriesAsync()'],480,870);
    add('IInteractionRepository',['<<interface>>'],940,870);
    add('InteractionRepository',['repository implementation'],1380,870);
    add('EventsController',['+ CreateEvent() / GetEvents()'],30,1160);
    add('EventService',['+ CreateEventAsync()','+ RsvpEventAsync()','+ GetAttendeesAsync()'],480,1160);
    add('IEventRepository',['<<interface>>'],940,1160);
    add('EventRepository',['repository implementation'],1380,1160);
    add('IKafkaProducerService',['<<interface>>','+ PublishPostCreatedAsync()'],30,1450);
    add('KafkaProducerService',['+ PublishPostCreatedAsync()'],480,1450);
    add('ForumServiceDbContext',['DbSet<Post>, DbSet<Comment>','+ SaveChangesAsync()'],1380,1450);
    p.edge('PostsController','PostService','uses');p.edge('PostService','IPostService','implements',{realize:true,dashed:true,sideA:'bottom',sideB:'top'});
    p.edge('PostService','IPostRepository','uses');p.edge('PostRepository','IPostRepository','implements',{realize:true,dashed:true});
    [['CommentsController','CommentService','ICommentRepository','CommentRepository','ICommentService'],['ReactionsController','InteractionService','IInteractionRepository','InteractionRepository','IInteractionService'],['EventsController','EventService','IEventRepository','EventRepository','IEventService']].forEach(([c,s,r,i,intf])=>{p.edge(c,s,'uses');p.edge(s,r,'uses');p.edge(i,r,'implements',{realize:true,dashed:true});});
    p.edge('KafkaProducerService','IKafkaProducerService','implements',{realize:true,dashed:true});
    p.note('Cross-dependencies: PostService → IInteractionRepository + IKafkaProducerService.\nCommentService / InteractionService → IPostRepository. Mỗi repository → ForumServiceDbContext.\nPosts/Comments/Reactions/Events controllers → IHubContext<ForumHub> (SignalR).\nCommentService, InteractionService, EventService implement interface cùng tên có tiền tố I.',30,1640,1730);
  }else{
    add('AiChatController',['+ Chat()','+ GetConversation()','+ GetConversations()','+ Ingest()'],30,140);
    add('AiChatService',['+ AskAsync()','+ GetConversationHistoryAsync()','+ GetUserConversationsAsync()'],480,140);
    add('IAiChatService',['<<interface>>'],480,380);
    add('IRagSearchService',['<<interface>>','+ SearchRelevantChunksAsync()'],940,140);
    add('RagSearchService',['+ SearchRelevantChunksAsync()'],1380,140);
    add('RagStartupWorker',['# ExecuteAsync()'],30,580);
    add('RagIngestionService',['+ IngestDocumentsFromDirectoryAsync()','+ IngestForumPostAsync()'],480,580,400);
    add('IRagIngestionService',['<<interface>>'],480,770,400);
    add('IRagRepository',['<<interface>>','+ GetAllChunksAsync()','+ GetConversationAsync()','+ CreateConversationAsync()','+ AddMessageAsync()','+ GetMentorsAsync()'],940,580);
    add('RagRepository',['+ GetAllChunksAsync()','+ AddMessageAsync()','+ GetMentorsAsync()'],1380,580);
    add('MentorsController',['+ Recommend()'],30,1030);
    add('MentorRecommendationService',['+ RecommendMentorsAsync()'],480,1030,400);
    add('IMentorRecommendationService',['<<interface>>'],480,1200,400);
    add('AiServiceDbContext',['DbSet<RagDocument> / RagChunk','DbSet<AiConversation> / AiMessage','DbSet<MentorProfile>','+ SaveChangesAsync()'],1380,1050);
    p.edge('AiChatController','AiChatService','uses');p.edge('AiChatService','IAiChatService','implements',{realize:true,dashed:true,sideA:'bottom',sideB:'top'});
    p.edge('AiChatService','IRagSearchService','uses');p.edge('RagSearchService','IRagSearchService','implements',{realize:true,dashed:true});
    p.edge('RagStartupWorker','RagIngestionService','uses');p.edge('RagIngestionService','IRagIngestionService','implements',{realize:true,dashed:true,sideA:'bottom',sideB:'top'});
    p.edge('RagIngestionService','IRagRepository','uses');p.edge('RagRepository','IRagRepository','implements',{realize:true,dashed:true});
    p.edge('MentorsController','MentorRecommendationService','service');p.edge('MentorRecommendationService','IMentorRecommendationService','implements',{realize:true,dashed:true,sideA:'bottom',sideB:'top'});
    p.edge('RagRepository','AiServiceDbContext','EF Core',{sideA:'bottom',sideB:'top'});
    p.note('Additional dependencies: AiChatController → IRagIngestionService. AiChatService → IRagRepository + IHttpClientFactory.\nRagSearchService / MentorRecommendationService → IRagRepository. RagStartupWorker resolves scoped services.\nGemini là HTTP API, không phải entity. RAG hiện tìm kiếm lexical + intent scoring, không có vector DB / embedding.\nIngestForumPostAsync đã có; Program.cs hiện không đăng ký Kafka consumer, nên chưa vẽ như một luồng đang chạy.',30,1440,1730);
  }
  return p;
}
function mermaid(entities,service,erd){
  if(erd){return 'erDiagram\n'+relations[service].map(([a,b,fk])=>`    ${entities.find(e=>e.name===a).table} ${fk==='ParentCommentId'?'|o':'||'}--o{ ${entities.find(e=>e.name===b).table} : "${fk}"`).join('\n')+'\n'+entities.map(e=>`    ${e.table} {\n${e.scalar.map(p=>`        ${p.sql.replace(/[()]/g,'_')} ${p.name}${p.name==='Id'?' PK':relations[service].some(r=>r[1]===e.name&&r[2]===p.name)?' FK':''}${p.nullable?' "NULL"':''}`).join('\n')}\n    }`).join('\n');}
  return 'classDiagram\n    direction TB\n'+entities.map(e=>`    class ${e.name} {\n${e.props.map(p=>`        +${p.type.replace(/</g,'~').replace(/>/g,'~')} ${p.name}`).join('\n')}\n    }`).join('\n')+'\n'+relations[service].map(([a,b,fk])=>`    ${a} "${fk==='ParentCommentId'?'0..1':'1'}" --> "0..*" ${b} : ${fk}`).join('\n');
}
function generate(service){
  const entities=entityData(service);
  const pages=[sequence(service),core(service),domain(service,entities,false),domain(service,entities,true)];
  for(const [i,p] of pages.entries())save(`${service}-${String(i+1).padStart(2,'0')}.svg`,p.svg());
  save(`${service}-Diagrams.drawio`,`<?xml version="1.0" encoding="UTF-8"?><mxfile host="app.diagrams.net" agent="Codex" version="24.7.17">${pages.map(p=>p.xml()).join('')}</mxfile>`);
  const notes=service==='Forum'?[
    'Forum DB theo appsettings: FAlumni_Forum_Db. ERD gồm đủ 11 bảng trong DbContext, bao gồm SampleItems.',
    'Kafka publish dùng fire-and-forget; lỗi được log. SignalR broadcast được await trước HTTP 201.',
    'Categories chưa liên kết FK tới Posts. Reports dùng TargetType + TargetId, không phải FK vật lý.',
    'Comment.ParentCommentId nullable, delete Restrict; các FK còn lại delete Cascade.'
  ]:[
    'AI DB theo appsettings: FAlumni_AiService_Db; repository fallback dùng FAlumni_AI_Db. ERD gồm 6 bảng.',
    'RAG dùng keyword/intent scoring, lọc Score > 5 rồi lấy Top 4. Chưa có vector database hoặc embedding.',
    'Lịch sử hội thoại được lưu/đọc nhưng GenerateWithGeminiAsync hiện chưa chèn history vào prompt.',
    'IngestForumPostAsync đã có; chưa có Kafka consumer đăng ký trong Program.cs. Đây là điểm tích hợp cần hoàn thiện.',
    'CitationsJson là JSON, không có FK tới RagChunks. Guest đang dùng chung một GUID theo controller.'
  ];
  const sources=service==='Forum'?['Api/Controllers/PostsController.cs','Services/Implementations/PostService.cs','Services/Implementations/KafkaProducerService.cs','Repositories/Implementations/PostRepository.cs','Repositories/Entities/*.cs','Repositories/Configurations/ForumConfiguration.cs','Repositories/Context/ForumServiceDbContext.cs']:['Api/Controllers/AiChatController.cs','Api/Program.cs','Api/Workers/RagStartupWorker.cs','Services/Implementations/AiChatService.cs','Services/Implementations/RagSearchService.cs','Services/Implementations/RagIngestionService.cs','Repositories/Implementations/RagRepository.cs','Repositories/Entities/AiEntities.cs','Repositories/Configurations/AiConfiguration.cs'];
  const html=`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>${service} · Diagrams</title><style>*{box-sizing:border-box}body{margin:0;background:#eef2f6;color:#0f172a;font:16px/1.55 'Segoe UI',Arial,sans-serif}header,main{max-width:1500px;margin:auto;padding:28px}header{background:${colors[service]};color:white;max-width:none}header>div{max-width:1440px;margin:auto}h1{margin:4px 0;font-size:34px}p{margin:8px 0}nav{display:flex;gap:10px;flex-wrap:wrap;margin-top:20px}a,button{color:inherit}nav a,button{border:1px solid #ffffff60;border-radius:8px;background:#ffffff18;padding:8px 15px;text-decoration:none;cursor:pointer}section{background:white;border-radius:14px;box-shadow:0 3px 15px #0f172a0b;padding:22px;margin:24px 0}h2{margin:0 0 10px}.canvas{overflow:auto;border:1px solid #dbe4ee;border-radius:8px;background:#f8fafc}.canvas svg{display:block;width:100%;height:auto;min-width:950px}.tools{display:flex;gap:8px;align-items:center;margin:12px 0}.tools button{border-color:#cbd5e1;background:white;color:#334155}.note{border-left:4px solid #f59e0b}li{margin:7px 0}details{margin-top:16px}pre{overflow:auto;background:#f1f5f9;padding:16px}footer{padding:24px;text-align:center;color:#64748b}@media print{header nav,.tools,details{display:none}body{background:white}main{padding:0}section{box-shadow:none;break-before:page;page-break-inside:avoid;padding:5mm;margin:0}.canvas{overflow:visible;border:0}.canvas svg{min-width:0;width:100%;max-height:260mm}header{print-color-adjust:exact}section.note{break-before:auto}h2{font-size:18px}@page{size:A3 landscape;margin:10mm}}</style><header><div><p>FPT ALUMNI CONNECT · PRN232</p><h1>${service} · Sequence / Class / ERD</h1><p>Đối chiếu source code tại D:\\ki8\\prn232 · 11/11/2026 · Xem offline, không cần mạng.</p><nav>${pages.map((p,i)=>`<a href="#page${i}">${esc(p.name)}</a>`).join('')}<a href="${service}-Diagrams.drawio" download>Download draw.io</a><button onclick="window.print()">In / Lưu PDF</button></nav></div></header><main><section class="note"><h2>Phạm vi và điểm cần lưu ý</h2><ul>${notes.map(n=>`<li>${esc(n)}</li>`).join('')}</ul><p>ERD phản ánh entities + Fluent API hiện tại, chưa xác minh schema/migrations đã được áp dụng trên SQL Server đang chạy. User identifiers chỉ là tham chiếu logic giữa các service.</p></section>${pages.map((p,i)=>`<section id="page${i}"><h2>${esc(p.name)}</h2><div class="tools"><button onclick="zoom(this,1.2)">Phóng to +</button><button onclick="zoom(this,1/1.2)">Thu nhỏ −</button><button onclick="resetZoom(this)">Vừa khung</button><a download href="data:image/svg+xml;charset=utf-8,${encodeURIComponent(p.svg())}">Tải SVG</a></div><div class="canvas">${p.svg()}</div>${i>1?`<details><summary>Mermaid source (copy để sửa)</summary><pre>${esc(mermaid(entities,service,i===3))}</pre></details>`:''}</section>`).join('')}<section><h2>Nguồn đối chiếu</h2><pre>${sources.map(s=>`${service==='AI'?'Ai':service}Service.${s}`).join('\n')}</pre><p>Bản draw.io có 4 trang: Sequence, Class Application, Class Domain và ERD. File SVG giữ nguyên nét và chữ khi phóng lớn.</p></section></main><footer>Source-grounded diagrams · 11 Nov 2026</footer><script>function zoom(b,k){let s=b.closest('section').querySelector('svg');let w=s.getBoundingClientRect().width*k;s.style.width=w+'px';s.style.minWidth='0'}function resetZoom(b){let s=b.closest('section').querySelector('svg');s.style.width='100%';s.style.minWidth='950px'}</script></html>`;
  save(`${service}-Diagrams.html`,html);
  save(`${service}-Diagrams.md`,`# ${service} — Diagrams\n\nSource snapshot: 2026-11-11.\n\n[Offline viewer](${service}-Diagrams.html) · [Editable diagrams.net](${service}-Diagrams.drawio)\n\n${notes.map(n=>'- '+n).join('\n')}\n\nERD reflects current EF Core entities and Fluent API; the live SQL Server schema has not been inspected.\n\n${pages.map((p,i)=>`## ${p.name}\n\n![${p.name}](${service}-${String(i+1).padStart(2,'0')}.svg)\n${i>1?'\n```mermaid\n'+mermaid(entities,service,i===3)+'\n```\n':''}`).join('\n')}\n## Source references\n\n${sources.map(s=>'- `'+(service==='AI'?'Ai':service)+'Service.'+s+'`').join('\n')}\n`);
  return {service,entities:entities.length,pages:pages.length};
}
const result=['Forum','AI'].map(generate);
save('README.md','# Forum + AI diagrams\n\nHai bộ sơ đồ được dựng từ source code, không gọi API hoặc kết nối database.\n\n| Module | Xem trực tiếp | Chỉnh sửa | Source / notes |\n| --- | --- | --- | --- |\n| Forum | [Forum-Diagrams.html](Forum-Diagrams.html) | [Forum-Diagrams.drawio](Forum-Diagrams.drawio) | [Forum-Diagrams.md](Forum-Diagrams.md) |\n| AI | [AI-Diagrams.html](AI-Diagrams.html) | [AI-Diagrams.drawio](AI-Diagrams.drawio) | [AI-Diagrams.md](AI-Diagrams.md) |\n\nMỗi bộ gồm 4 trang: Sequence Diagram, Class Diagram ứng dụng, Class Diagram entities và ERD.\n\nMở HTML bằng Chrome/Edge để xem offline và tải SVG; mở `.drawio` trong diagrams.net để chỉnh sửa.\n\nChạy lại bằng `node docs/forum-ai-diagrams/generate-diagrams.cjs` từ AI repo; cần hai repo Forum và AI cạnh nhau. Không sửa các bản sinh tự động; sửa generator rồi chạy lại. Không thay đổi runtime code, migrations hoặc tài liệu cũ.\n');
console.log(JSON.stringify(result));


