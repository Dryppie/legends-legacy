export interface GuideSection {
  heading: string;
  body: string;
  feature?: 'raids';
}

export interface Guide {
  title: string;
  lastReviewed: string;
  sections: GuideSection[];
}
